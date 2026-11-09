using ControleEstoque.DAO;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;

namespace ControleEstoque.Services;

/// <summary>
/// Validação de entrada (defesa no servidor) + orquestração.
/// A regra que depende do estado do estoque (disponível suficiente) é aplicada no DAO,
/// dentro da transação com lock, para ser atômica.
/// </summary>
public class EstoqueService
{
    public const int QuantidadeMaxima = 1_000_000;

    private readonly IEstoqueDAO _dao;

    public EstoqueService(IEstoqueDAO dao) => _dao = dao;

    public void Entrada(int produtoId, int quantidade)
    {
        ValidarProduto(produtoId); ValidarQuantidade(quantidade);
        _dao.Entrada(produtoId, quantidade);
    }

    public void Saida(int produtoId, int quantidade)
    {
        ValidarProduto(produtoId); ValidarQuantidade(quantidade);
        _dao.Saida(produtoId, quantidade);
    }

    public RegistroCriado Reservar(int produtoId, int quantidade, string? solicitacaoId)
    {
        ValidarProduto(produtoId); ValidarQuantidade(quantidade);
        var solicitacao = (solicitacaoId ?? string.Empty).Trim();
        if (solicitacao.Length == 0)
            throw new RegraNegocioException("Informe a solicitação/pedido da reserva.");
        if (solicitacao.Length > 50)
            throw new RegraNegocioException("A solicitação deve ter no máximo 50 caracteres.");
        return _dao.Reservar(produtoId, quantidade, solicitacao);
    }

    public RegistroCriado Bloquear(int produtoId, int quantidade, string? motivo)
    {
        ValidarProduto(produtoId); ValidarQuantidade(quantidade);
        var m = (motivo ?? string.Empty).Trim();
        if (m.Length > 200)
            throw new RegraNegocioException("O motivo deve ter no máximo 200 caracteres.");
        return _dao.Bloquear(produtoId, quantidade, m);
    }

    public int LiberarReserva(int reservaId, DesfechoLiberacao desfecho)
    {
        ValidarId(reservaId, "Reserva"); ValidarDesfecho(desfecho);
        return _dao.LiberarReserva(reservaId, desfecho);
    }

    public int LiberarBloqueio(int bloqueioId, DesfechoLiberacao desfecho)
    {
        ValidarId(bloqueioId, "Bloqueio"); ValidarDesfecho(desfecho);
        return _dao.LiberarBloqueio(bloqueioId, desfecho);
    }

    private static void ValidarProduto(int id) => ValidarId(id, "Produto");

    private static void ValidarId(int id, string nome)
    {
        if (id <= 0) throw new RegraNegocioException($"{nome} inválido.");
    }

    private static void ValidarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
            throw new RegraNegocioException("A quantidade deve ser maior que zero.");
        if (quantidade > QuantidadeMaxima)
            throw new RegraNegocioException($"A quantidade máxima por operação é {QuantidadeMaxima:N0}.");
    }

    private static void ValidarDesfecho(DesfechoLiberacao d)
    {
        // enum aceita qualquer inteiro vindo do binder; valida explicitamente
        if (!Enum.IsDefined(d)) throw new RegraNegocioException("Ação de liberação inválida.");
    }
}
