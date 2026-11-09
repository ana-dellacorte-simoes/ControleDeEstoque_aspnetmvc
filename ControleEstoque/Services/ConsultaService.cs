using ControleEstoque.DAO;
using ControleEstoque.Models;

namespace ControleEstoque.Services;

/// <summary>Consultas somente leitura (listas e dashboard).</summary>
public class ConsultaService
{
    private readonly ProdutosDAO _produtos;
    private readonly ReservasDAO _reservas;
    private readonly BloqueiosDAO _bloqueios;
    private readonly MovimentacoesDAO _movimentacoes;

    public ConsultaService(ProdutosDAO p, ReservasDAO r, BloqueiosDAO b, MovimentacoesDAO m)
        => (_produtos, _reservas, _bloqueios, _movimentacoes) = (p, r, b, m);

    public List<Reserva> ListarReservas(string? busca, bool historico) => _reservas.Listar(busca, historico);
    public List<Bloqueio> ListarBloqueios(string? busca, bool historico) => _bloqueios.Listar(busca, historico);

    public DashboardViewModel ObterDashboard()
    {
        var ativos = _produtos.ConsultarEstoque().Where(p => p.Ativo).ToList();
        return new DashboardViewModel
        {
            TotalProdutos = ativos.Count,
            Total = ativos.Sum(p => (long)p.QuantidadeTotal),
            Disponivel = ativos.Sum(p => (long)p.QuantidadeDisponivel),
            Reservado = ativos.Sum(p => (long)p.QuantidadeReservada),
            Bloqueado = ativos.Sum(p => (long)p.QuantidadeBloqueada),
            Atencao = ativos.OrderBy(p => p.QuantidadeTotal == 0 ? 1.0 : (double)p.QuantidadeDisponivel / p.QuantidadeTotal)
                            .ThenBy(p => p.Nome).Take(5).ToList(),
            Recentes = _movimentacoes.Recentes(10)
        };
    }
}
