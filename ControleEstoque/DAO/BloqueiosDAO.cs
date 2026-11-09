using ControleEstoque.Models;
using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO;

public class BloqueiosDAO
{
    private readonly ConexaoBD _conexaoBD;
    public BloqueiosDAO(ConexaoBD conexaoBD) => _conexaoBD = conexaoBD;

    public List<Bloqueio> Listar(string? busca, bool incluirHistorico)
    {
        var lista = new List<Bloqueio>();
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var cmd = new SqlCommand("""
            SELECT TOP (500) b.Id, b.ProdutoId, p.Nome, b.Quantidade, b.Motivo,
                   b.DataCriacao, b.Ativo, b.Desfecho, b.DataLiberacao
            FROM Bloqueios b
            JOIN Produtos p ON p.Id = b.ProdutoId
            WHERE (@Historico = 1 OR b.Ativo = 1)
              AND (@Busca IS NULL OR p.Nome LIKE @Busca ESCAPE '\' OR b.Motivo LIKE @Busca ESCAPE '\')
            ORDER BY b.Ativo DESC, b.DataCriacao DESC, b.Id DESC
            """, conexao);
        cmd.Int("@Historico", incluirHistorico ? 1 : 0);
        cmd.Varchar("@Busca", 210, SqlHelper.PadraoLike(busca));

        using var l = cmd.ExecuteReader();
        while (l.Read())
        {
            lista.Add(new Bloqueio
            {
                IdBloqueio = l.GetInt32(0),
                IdProduto = l.GetInt32(1),
                ProdutoNome = l.GetString(2),
                Quantidade = l.GetInt32(3),
                Motivo = l.GetString(4),
                DataCriacao = l.GetDateTime(5),
                Ativo = l.GetBoolean(6),
                Desfecho = l.IsDBNull(7) ? null : l.GetString(7),
                DataLiberacao = l.IsDBNull(8) ? null : l.GetDateTime(8)
            });
        }
        return lista;
    }
}
