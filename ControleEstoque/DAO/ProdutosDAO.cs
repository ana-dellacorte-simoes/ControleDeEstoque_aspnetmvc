using System.Data;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO;

public class ProdutosDAO
{
    private readonly ConexaoBD _conexaoBD;

    public ProdutosDAO(ConexaoBD conexaoBD) => _conexaoBD = conexaoBD;

    public List<Produtos> Listar()
    {
        var produtos = new List<Produtos>();
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();

        using var comando = new SqlCommand(
            "SELECT Id, Nome, QuantidadeTotal, Ativo FROM Produtos ORDER BY Nome", conexao);
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            produtos.Add(new Produtos
            {
                IdProduto = leitor.GetInt32(0),
                Nome = leitor.GetString(1),
                QuantidadeTotal = leitor.GetInt32(2),
                Ativo = leitor.GetBoolean(3)
            });
        }
        return produtos;
    }

    /// <summary>Insere o produto e registra a entrada inicial na mesma transação.</summary>
    public int Inserir(Produtos produto)
    {
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var tx = conexao.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand("""
                INSERT INTO Produtos (Nome, QuantidadeTotal, Ativo)
                OUTPUT INSERTED.Id
                VALUES (@Nome, @QuantidadeTotal, @Ativo);
                """, conexao, tx);
            cmd.Varchar("@Nome", 100, produto.Nome);
            cmd.Int("@QuantidadeTotal", produto.QuantidadeTotal);
            cmd.Parameters.Add("@Ativo", SqlDbType.Bit).Value = produto.Ativo;
            int id = Convert.ToInt32(cmd.ExecuteScalar());

            if (produto.QuantidadeTotal > 0)
            {
                using var mov = new SqlCommand("""
                    INSERT INTO Movimentacoes (ProdutoId, Tipo, Quantidade, Referencia)
                    VALUES (@P, 'Entrada', @Q, 'Cadastro do produto')
                    """, conexao, tx);
                mov.Int("@P", id);
                mov.Int("@Q", produto.QuantidadeTotal);
                mov.ExecuteNonQuery();
            }

            tx.Commit();
            return id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            tx.Rollback();
            throw new RegraNegocioException("Já existe um produto com esse nome.");
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Desativação lógica (mantém no histórico). Só permitida quando não há quantidade
    /// disponível, reservada nem bloqueada — ou seja, produto "zerado" e sem pendências.
    /// Trava a linha do produto para não conflitar com uma reserva/bloqueio sendo criada ao mesmo tempo.
    /// </summary>
    public void Desativar(int produtoId)
    {
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var tx = conexao.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            int total;
            bool ativo;
            using (var cmd = new SqlCommand(
                "SELECT QuantidadeTotal, Ativo FROM Produtos WITH (UPDLOCK, ROWLOCK) WHERE Id = @Id", conexao, tx))
            {
                cmd.Int("@Id", produtoId);
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                    throw new RecursoNaoEncontradoException("Produto não encontrado.");
                total = r.GetInt32(0);
                ativo = r.GetBoolean(1);
            }

            if (!ativo)
                throw new RegraNegocioException("O produto já está inativo.");

            using var cmdPendencias = new SqlCommand("""
                SELECT
                  COALESCE((SELECT SUM(Quantidade) FROM Reservas  WHERE ProdutoId = @Id AND Ativa = 1), 0),
                  COALESCE((SELECT SUM(Quantidade) FROM Bloqueios WHERE ProdutoId = @Id AND Ativo = 1), 0)
                """, conexao, tx);
            cmdPendencias.Int("@Id", produtoId);
            int reservado, bloqueado;
            using (var r = cmdPendencias.ExecuteReader())
            {
                r.Read();
                reservado = r.GetInt32(0);
                bloqueado = r.GetInt32(1);
            }

            if (reservado > 0 || bloqueado > 0)
                throw new RegraNegocioException(
                    "Só é possível desativar um produto sem reservas nem bloqueios ativos.");
            if (total - reservado - bloqueado != 0)
                throw new RegraNegocioException(
                    "Só é possível desativar um produto com quantidade disponível igual a zero.");

            using (var cmd = new SqlCommand(
                "UPDATE Produtos SET Ativo = 0 WHERE Id = @Id AND Ativo = 1", conexao, tx))
            {
                cmd.Int("@Id", produtoId);
                if (cmd.ExecuteNonQuery() != 1)
                    throw new RegraNegocioException("Não foi possível desativar o produto.");
            }

            tx.Commit();
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    public List<EstoqueProduto> ConsultarEstoque(string? busca = null) => Consultar(busca, null);

    public EstoqueProduto? ObterEstoque(int id) => Consultar(null, id).FirstOrDefault();

    private List<EstoqueProduto> Consultar(string? busca, int? id)
    {
        var produtos = new List<EstoqueProduto>();
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();

        using var cmd = new SqlCommand("""
            SELECT p.Id, p.Nome, p.QuantidadeTotal, p.Ativo,
                   COALESCE(r.TotalReservado, 0), COALESCE(b.TotalBloqueado, 0)
            FROM Produtos p
            OUTER APPLY (SELECT SUM(Quantidade) AS TotalReservado FROM Reservas
                         WHERE ProdutoId = p.Id AND Ativa = 1) r
            OUTER APPLY (SELECT SUM(Quantidade) AS TotalBloqueado FROM Bloqueios
                         WHERE ProdutoId = p.Id AND Ativo = 1) b
            WHERE (@Id IS NULL OR p.Id = @Id)
              AND (@Busca IS NULL OR p.Nome LIKE @Busca ESCAPE '\')
            ORDER BY p.Nome
            """, conexao);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id.HasValue ? id.Value : DBNull.Value;
        cmd.Varchar("@Busca", 210, SqlHelper.PadraoLike(busca));

        using var leitor = cmd.ExecuteReader();
        while (leitor.Read())
        {
            produtos.Add(new EstoqueProduto
            {
                Id = leitor.GetInt32(0),
                Nome = leitor.GetString(1),
                QuantidadeTotal = leitor.GetInt32(2),
                Ativo = leitor.GetBoolean(3),
                QuantidadeReservada = leitor.GetInt32(4),
                QuantidadeBloqueada = leitor.GetInt32(5)
            });
        }
        return produtos;
    }
}
