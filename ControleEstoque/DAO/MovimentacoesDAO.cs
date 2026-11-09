using ControleEstoque.Models;
using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO;

public class MovimentacoesDAO
{
    private readonly ConexaoBD _conexaoBD;
    public MovimentacoesDAO(ConexaoBD conexaoBD) => _conexaoBD = conexaoBD;

    public List<Movimentacao> Recentes(int quantidade)
    {
        var lista = new List<Movimentacao>();
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var cmd = new SqlCommand("""
            SELECT TOP (@N) m.Id, m.ProdutoId, p.Nome, m.Tipo, m.Quantidade, m.Referencia, m.DataCriacao
            FROM Movimentacoes m
            JOIN Produtos p ON p.Id = m.ProdutoId
            ORDER BY m.Id DESC
            """, conexao);
        cmd.Int("@N", Math.Clamp(quantidade, 1, 200));

        using var l = cmd.ExecuteReader();
        while (l.Read())
        {
            lista.Add(new Movimentacao
            {
                Id = l.GetInt32(0),
                IdProduto = l.GetInt32(1),
                ProdutoNome = l.GetString(2),
                Tipo = l.GetString(3),
                Quantidade = l.GetInt32(4),
                Referencia = l.IsDBNull(5) ? null : l.GetString(5),
                DataCriacao = l.GetDateTime(6)
            });
        }
        return lista;
    }

    /// <summary>Movimentações em um período (datas em UTC; limite superior exclusivo), mais recentes primeiro.</summary>
    public List<Movimentacao> ListarPeriodo(DateTime? deUtc, DateTime? ateUtcExclusivo, int limite = 5000)
    {
        var lista = new List<Movimentacao>();
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var cmd = new SqlCommand("""
            SELECT TOP (@N) m.Id, m.ProdutoId, p.Nome, m.Tipo, m.Quantidade, m.Referencia, m.DataCriacao
            FROM Movimentacoes m
            JOIN Produtos p ON p.Id = m.ProdutoId
            WHERE (@De IS NULL OR m.DataCriacao >= @De)
              AND (@Ate IS NULL OR m.DataCriacao < @Ate)
            ORDER BY m.Id DESC
            """, conexao);
        cmd.Int("@N", Math.Clamp(limite, 1, 20000));
        cmd.Parameters.Add("@De", System.Data.SqlDbType.DateTime2).Value = deUtc.HasValue ? deUtc.Value : DBNull.Value;
        cmd.Parameters.Add("@Ate", System.Data.SqlDbType.DateTime2).Value = ateUtcExclusivo.HasValue ? ateUtcExclusivo.Value : DBNull.Value;

        using var l = cmd.ExecuteReader();
        while (l.Read())
        {
            lista.Add(new Movimentacao
            {
                Id = l.GetInt32(0),
                IdProduto = l.GetInt32(1),
                ProdutoNome = l.GetString(2),
                Tipo = l.GetString(3),
                Quantidade = l.GetInt32(4),
                Referencia = l.IsDBNull(5) ? null : l.GetString(5),
                DataCriacao = l.GetDateTime(6)
            });
        }
        return lista;
    }
}
