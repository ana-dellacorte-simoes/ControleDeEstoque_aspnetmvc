using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using ControleEstoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleEstoque.Controllers;

public class ProdutosController : Controller
{
    private readonly ProdutosService _produtos;
    private readonly EstoqueService _estoque;

    public ProdutosController(ProdutosService produtos, EstoqueService estoque)
    {
        _produtos = produtos;
        _estoque = estoque;
    }

    [HttpGet]
    public IActionResult Index(string? q)
        => View(new ListaEstoqueViewModel { Busca = q, Itens = _produtos.ConsultarEstoque(q) });

    // ---------------------------------------------------------- Cadastro
    [HttpGet]
    public IActionResult Cadastrar() => View(new ProdutoCadastroViewModel());

    [HttpPost]
    public IActionResult Cadastrar(ProdutoCadastroViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            int id = _produtos.Cadastrar(new Produtos { Nome = vm.Nome, QuantidadeTotal = vm.QuantidadeInicial });
            TempData["Sucesso"] = $"Produto \"{vm.Nome.Trim()}\" cadastrado com sucesso. ID: {id}.";
            return RedirectToAction(nameof(Index));
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    public IActionResult Desativar(int id)
    {
        var produto = _produtos.Consultar(id);
        var nome = produto?.Nome ?? $"#{id}";
        try
        {
            _produtos.Desativar(id);
            TempData["Sucesso"] = $"Produto \"{nome}\" desativado. Ele permanece no histórico, mas some das listas de uso.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------- Entrada / Saída
    [HttpGet]
    public IActionResult Entrada(int? produtoId) => FormMovimento("Entrada", produtoId);

    [HttpGet]
    public IActionResult Saida(int? produtoId) => FormMovimento("Saida", produtoId);

    [HttpPost]
    public IActionResult Entrada(MovimentoViewModel vm) => ProcessarMovimento("Entrada", vm);

    [HttpPost]
    public IActionResult Saida(MovimentoViewModel vm) => ProcessarMovimento("Saida", vm);

    private IActionResult FormMovimento(string tipo, int? produtoId)
    {
        var vm = new MovimentoViewModel { Tipo = tipo, ProdutoId = produtoId ?? 0 };
        vm.Produtos = _produtos.ConsultarEstoque().Where(p => p.Ativo).ToList();
        return View("Movimento", vm);
    }

    private IActionResult ProcessarMovimento(string tipo, MovimentoViewModel vm)
    {
        vm.Tipo = tipo;
        vm.Produtos = _produtos.ConsultarEstoque().Where(p => p.Ativo).ToList();
        if (!ModelState.IsValid) return View("Movimento", vm);

        try
        {
            var nome = vm.Produtos.FirstOrDefault(p => p.Id == vm.ProdutoId)?.Nome ?? $"#{vm.ProdutoId}";
            if (tipo == "Entrada")
            {
                _estoque.Entrada(vm.ProdutoId, vm.Quantidade);
                TempData["Sucesso"] = $"Entrada registrada: +{vm.Quantidade} un. de \"{nome}\".";
            }
            else
            {
                _estoque.Saida(vm.ProdutoId, vm.Quantidade);
                TempData["Sucesso"] = $"Saída registrada: -{vm.Quantidade} un. de \"{nome}\".";
            }
            return RedirectToAction(nameof(Index));
        }
        catch (EstoqueInsuficienteException ex)
        {
            ModelState.AddModelError(nameof(vm.Quantidade), ex.Message);
            ViewData["Insuficiente"] = true;
            return View("Movimento", vm);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Movimento", vm);
        }
    }
}
