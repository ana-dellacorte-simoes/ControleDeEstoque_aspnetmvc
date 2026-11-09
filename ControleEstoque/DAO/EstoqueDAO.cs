using System.Data;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO;

/// <summary>
/// Todas as escritas de estoque. Estratégia de concorrência:
///   1) abre transação;
///   2) trava a linha do produto (UPDLOCK) => operações do MESMO produto são serializadas;
///   3) lê reservado/bloqueado já com o lock e valida a regra (Disponível = Total − Reservado − Bloqueado);
///   4) grava + registra movimentação; COMMIT.
/// Qualquer exceção => ROLLBACK: operação inválida nunca deixa o estoque inconsistente.
/// Produtos diferentes não se bloqueiam entre si.
/// </summary>
public class EstoqueDAO : IEstoqueDAO
{
    public const int LimiteTotal = 1_000_000_000;

    private readonly ConexaoBD _conexaoBD;

    public EstoqueDAO(ConexaoBD conexaoBD) => _conexaoBD = conexaoBD;

    private readonly record struct Situacao(int Total, int Reservado, int Bloqueado)
    {
        public int Disponivel => Total - Reservado - Bloqueado;
    }

    private T Executar<T>(Func<SqlConnection, SqlTransaction, T> operacao)
    {
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var transacao = conexao.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var resultado = operacao(conexao, transacao);
            transacao.Commit();
            return resultado;
        }
        catch
        {
            try { transacao.Rollback(); } catch { /* conexão já invalidada */ }
            throw;
        }
    }

    // ---------------------------------------------------------------- Entrada
    public void Entrada(int produtoId, int quantidade) => Executar<object?>((c, tx) =>
    {
        var s = TravarESituacao(c, tx, produtoId);
        if ((long)s.Total + quantidade > LimiteTotal)
            throw new RegraNegocioException("A entrada ultrapassa o limite máximo de estoque do produto.");

        AtualizarTotal(c, tx, produtoId, +quantidade);
        InserirMovimentacao(c, tx, produtoId, "Entrada", quantidade, null);
        return null;
    });

    // ------------------------------------------------------------------ Saída
    public void Saida(int produtoId, int quantidade) => Executar<object?>((c, tx) =>
    {
        var s = TravarESituacao(c, tx, produtoId);
        if (quantidade > s.Disponivel)
            throw new EstoqueInsuficienteException(s.Disponivel);

        AtualizarTotal(c, tx, produtoId, -quantidade);
        InserirMovimentacao(c, tx, produtoId, "Saida", quantidade, null);
        return null;
    });

    // ---------------------------------------------------------------- Reservar
    public RegistroCriado Reservar(int produtoId, int quantidade, string solicitacaoId) => Executar((c, tx) =>
    {
        var s = TravarESituacao(c, tx, produtoId);
        if (quantidade > s.Disponivel)
            throw new EstoqueInsuficienteException(s.Disponivel);

        const string sql = """
            INSERT INTO Reservas (ProdutoId, SolicitacaoId, Quantidade)
            OUTPUT INSERTED.Id, INSERTED.DataCriacao
            VALUES (@ProdutoId, @SolicitacaoId, @Quantidade);
            """;
        using var cmd = new SqlCommand(sql, c, tx);
        cmd.Int("@ProdutoId", produtoId);
        cmd.Varchar("@SolicitacaoId", 50, solicitacaoId);
        cmd.Int("@Quantidade", quantidade);

        using var r = cmd.ExecuteReader();
        r.Read();
        var criado = new RegistroCriado(r.GetInt32(0), r.GetDateTime(1), quantidade);
        r.Close();

        InserirMovimentacao(c, tx, produtoId, "Reserva", quantidade,
            $"Reserva #{criado.Id} - solicitação {solicitacaoId}");
        return criado;
    });

    // ---------------------------------------------------------------- Bloquear
    public RegistroCriado Bloquear(int produtoId, int quantidade, string motivo) => Executar((c, tx) =>
    {
        var s = TravarESituacao(c, tx, produtoId);
        if (quantidade > s.Disponivel)
            throw new EstoqueInsuficienteException(s.Disponivel);

        const string sql = """
            INSERT INTO Bloqueios (ProdutoId, Quantidade, Motivo)
            OUTPUT INSERTED.Id, INSERTED.DataCriacao
            VALUES (@ProdutoId, @Quantidade, @Motivo);
            """;
        using var cmd = new SqlCommand(sql, c, tx);
        cmd.Int("@ProdutoId", produtoId);
        cmd.Int("@Quantidade", quantidade);
        cmd.Varchar("@Motivo", 200, motivo);

        using var r = cmd.ExecuteReader();
        r.Read();
        var criado = new RegistroCriado(r.GetInt32(0), r.GetDateTime(1), quantidade);
        r.Close();

        InserirMovimentacao(c, tx, produtoId, "Bloqueio", quantidade,
            string.IsNullOrEmpty(motivo) ? $"Bloqueio #{criado.Id}" : $"Bloqueio #{criado.Id} - {Truncar(motivo, 100)}");
        return criado;
    });

    // ----------------------------------------------------------------- Liberar
    public int LiberarReserva(int reservaId, DesfechoLiberacao desfecho) => Liberar(true, reservaId, desfecho);
    public int LiberarBloqueio(int bloqueioId, DesfechoLiberacao desfecho) => Liberar(false, bloqueioId, desfecho);

    private int Liberar(bool reserva, int id, DesfechoLiberacao desfecho) => Executar((c, tx) =>
    {
        // Nomes de tabela/coluna são CONSTANTES do código (nunca vêm do usuário); valores sempre parametrizados.
        string tabela = reserva ? "Reservas" : "Bloqueios";
        string colAtivo = reserva ? "Ativa" : "Ativo";
        string rotulo = reserva ? "Reserva" : "Bloqueio";

        // 1) descobre o produto (sem lock) para poder travá-lo
        int produtoId;
        using (var cmd = new SqlCommand($"SELECT ProdutoId FROM {tabela} WHERE Id = @Id", c, tx))
        {
            cmd.Int("@Id", id);
            var o = cmd.ExecuteScalar();
            if (o is null) throw new RecursoNaoEncontradoException($"{rotulo} #{id} não encontrada.");
            produtoId = Convert.ToInt32(o);
        }

        // 2) trava o produto => serializa com qualquer outra operação nele
        TravarESituacao(c, tx, produtoId);

        // 3) baixa condicional: só uma transação consegue passar de Ativa=1 para 0 (impede liberação duplicada)
        int quantidade;
        using (var cmd = new SqlCommand($"""
            UPDATE {tabela}
               SET {colAtivo} = 0, Desfecho = @Desfecho, DataLiberacao = SYSUTCDATETIME()
            OUTPUT INSERTED.Quantidade
             WHERE Id = @Id AND {colAtivo} = 1
            """, c, tx))
        {
            cmd.Int("@Id", id);
            cmd.Varchar("@Desfecho", 20, desfecho == DesfechoLiberacao.Retirar ? "Retirada" : "Devolvida");
            var o = cmd.ExecuteScalar();
            if (o is null) throw new RegraNegocioException($"{rotulo} #{id} já foi liberada anteriormente.");
            quantidade = Convert.ToInt32(o);
        }

        // 4) Retirar => unidades saem definitivamente do total. Devolver => voltam a ficar disponíveis (nada a somar:
        //    disponível é derivado de Total − ativos).
        if (desfecho == DesfechoLiberacao.Retirar)
            AtualizarTotal(c, tx, produtoId, -quantidade);

        string tipo = rotulo + (desfecho == DesfechoLiberacao.Retirar
            ? (reserva ? "Retirada" : "Retirado")
            : (reserva ? "Devolvida" : "Devolvido"));
        InserirMovimentacao(c, tx, produtoId, tipo, quantidade, $"{rotulo} #{id}");
        return quantidade;
    });

    // --------------------------------------------------------------- helpers
    private static Situacao TravarESituacao(SqlConnection c, SqlTransaction tx, int produtoId)
    {
        int total;
        using (var cmd = new SqlCommand(
            "SELECT QuantidadeTotal FROM Produtos WITH (UPDLOCK, ROWLOCK) WHERE Id = @Id AND Ativo = 1", c, tx))
        {
            cmd.Int("@Id", produtoId);
            var o = cmd.ExecuteScalar();
            if (o is null) throw new RecursoNaoEncontradoException("Produto não encontrado ou inativo.");
            total = Convert.ToInt32(o);
        }

        // Lido DEPOIS do lock (statement separado) => valores estáveis até o fim da transação.
        using var cmd2 = new SqlCommand("""
            SELECT
              COALESCE((SELECT SUM(Quantidade) FROM Reservas  WHERE ProdutoId = @Id AND Ativa = 1), 0),
              COALESCE((SELECT SUM(Quantidade) FROM Bloqueios WHERE ProdutoId = @Id AND Ativo = 1), 0)
            """, c, tx);
        cmd2.Int("@Id", produtoId);
        using var r = cmd2.ExecuteReader();
        r.Read();
        return new Situacao(total, r.GetInt32(0), r.GetInt32(1));
    }

    private static void AtualizarTotal(SqlConnection c, SqlTransaction tx, int produtoId, int delta)
    {
        using var cmd = new SqlCommand("""
            UPDATE Produtos SET QuantidadeTotal = QuantidadeTotal + @Delta
            WHERE Id = @Id AND QuantidadeTotal + @Delta >= 0
            """, c, tx);
        cmd.Int("@Id", produtoId);
        cmd.Int("@Delta", delta);
        if (cmd.ExecuteNonQuery() != 1)
            throw new RegraNegocioException("Não foi possível atualizar o estoque.");
    }

    private static void InserirMovimentacao(SqlConnection c, SqlTransaction tx, int produtoId, string tipo, int quantidade, string? referencia)
    {
        using var cmd = new SqlCommand(
            "INSERT INTO Movimentacoes (ProdutoId, Tipo, Quantidade, Referencia) VALUES (@P, @T, @Q, @R)", c, tx);
        cmd.Int("@P", produtoId);
        cmd.Varchar("@T", 30, tipo);
        cmd.Int("@Q", quantidade);
        cmd.Varchar("@R", 150, referencia is null ? null : Truncar(referencia, 150));
        cmd.ExecuteNonQuery();
    }

    private static string Truncar(string s, int max) => s.Length <= max ? s : s[..max];
}
