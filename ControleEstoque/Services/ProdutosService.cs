using ControleEstoque.DAO;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;

namespace ControleEstoque.Services;

public class ProdutosService
{
    private readonly ProdutosDAO _produtoDAO;

    public ProdutosService(ProdutosDAO produtoDAO) => _produtoDAO = produtoDAO;

    public int Cadastrar(Produtos produto)
    {
        var nome = (produto.Nome ?? string.Empty).Trim();
        if (nome.Length == 0)
            throw new RegraNegocioException("O nome do produto é obrigatório.");
        if (nome.Length > 100)
            throw new RegraNegocioException("O nome do produto deve ter no máximo 100 caracteres.");
        if (produto.QuantidadeTotal < 0)
            throw new RegraNegocioException("A quantidade não pode ser negativa.");
        if (produto.QuantidadeTotal > EstoqueService.QuantidadeMaxima)
            throw new RegraNegocioException($"A quantidade inicial máxima é {EstoqueService.QuantidadeMaxima:N0}.");

        // Entidade nova montada aqui: o cliente nunca define Id/Ativo.
        return _produtoDAO.Inserir(new Produtos { Nome = nome, QuantidadeTotal = produto.QuantidadeTotal, Ativo = true });
    }

    public List<Produtos> Listar() => _produtoDAO.Listar();

    public List<EstoqueProduto> ConsultarEstoque(string? busca = null) => _produtoDAO.ConsultarEstoque(busca);

    public EstoqueProduto? Consultar(int id) => _produtoDAO.ObterEstoque(id);

    /// <summary>Desativação lógica: produto permanece no histórico, mas sai das listas de uso.
    /// Exige disponível = 0 e nenhuma reserva ou bloqueio ativos (checado atomicamente no DAO).</summary>
    public void Desativar(int id)
    {
        if (id <= 0) throw new RegraNegocioException("Produto inválido.");
        _produtoDAO.Desativar(id);
    }
}
