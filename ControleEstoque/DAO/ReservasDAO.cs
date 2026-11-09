using System.Data;
using ControleEstoque.Models;
using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO;

public class ReservasDAO
{
    private readonly ConexaoBD _conexaoBD;
    public ReservasDAO(ConexaoBD conexaoBD) => _conexaoBD = conexaoBD;

    public List<Reserva> Listar(string? busca, bool incluirHistorico)
    {
        var lista = new List<Reserva>();
        using var conexao = _conexaoBD.CriarConexao();
        conexao.Open();
        using var cmd = new SqlCommand("""
            SELECT TOP (500) r.Id, r.ProdutoId, p.Nome, r.SolicitacaoId, r.Quantidade,
                   r.DataCriacao, r.Ativa, r.Desfecho, r.DataLiberacao
            FROM Reservas r
            JOIN Produtos p ON p.Id = r.ProdutoId
            WHERE (@Historico = 1 OR r.Ativa = 1)
              AND (@Busca IS NULL OR p.Nome LIKE @Busca ESCAPE '\' OR r.SolicitacaoId LIKE @Busca ESCAPE '\')
            ORDER BY r.Ativa DESC, r.DataCriacao DESC, r.Id DESC
            """, conexao);
        cmd.Int("@Historico", incluirHistorico ? 1 : 0);
        cmd.Varchar("@Busca", 210, SqlHelper.PadraoLike(busca));

        using var l = cmd.ExecuteReader();
        while (l.Read())
        {
            lista.Add(new Reserva
            {
                IdReserva = l.GetInt32(0),
                IdProduto = l.GetInt32(1),
                ProdutoNome = l.GetString(2),
                SolicitacaoId = l.GetString(3),
                Quantidade = l.GetInt32(4),
                DataCriacao = l.GetDateTime(5),
                Ativa = l.GetBoolean(6),
                Desfecho = l.IsDBNull(7) ? null : l.GetString(7),
                DataLiberacao = l.IsDBNull(8) ? null : l.GetDateTime(8)
            });
        }
        return lista;
    }
}
