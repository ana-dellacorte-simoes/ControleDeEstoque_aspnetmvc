using System.Globalization;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using ControleEstoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleEstoque.Controllers;

public class BloqueiosController : Controller
{
    private readonly EstoqueService _estoque;
    private readonly ConsultaService _consulta;
    private readonly ProdutosService _produtos;

    public BloqueiosController(EstoqueService estoque, ConsultaService consulta, ProdutosService produtos)
        => (_estoque, _consulta, _produtos) = (estoque, consulta, produtos);

    [HttpGet]
    public IActionResult Index(string? q, bool historico = false)
        => View(new ListaBloqueiosViewModel { Busca = q, Historico = historico, Itens = _consulta.ListarBloqueios(q, historico) });

    [HttpGet]
    public IActionResult Criar(int? produtoId)
        => View(Carregar(new BloqueioViewModel { ProdutoId = produtoId ?? 0 }));

    [HttpPost]
    public IActionResult Criar(BloqueioViewModel vm)
    {
        Carregar(vm);
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var b = _estoque.Bloquear(vm.ProdutoId, vm.Quantidade, vm.Motivo);
            var nome = vm.Produtos.FirstOrDefault(p => p.Id == vm.ProdutoId)?.Nome ?? $"#{vm.ProdutoId}";
            TempData["Sucesso"] = $"Bloqueio de \"{nome}\" realizado. ID do bloqueio: {b.Id} · " +
                $"Data: {b.DataCriacao.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)} · Quantidade: {b.Quantidade}.";
            return RedirectToAction(nameof(Index));
        }
        catch (EstoqueInsuficienteException ex)
        {
            ModelState.AddModelError(nameof(vm.Quantidade), ex.Message);
            ViewData["Insuficiente"] = true;
            return View(vm);
        }
        catch (RegraNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    public IActionResult Liberar(int id, DesfechoLiberacao acao)
    {
        try
        {
            int q = _estoque.LiberarBloqueio(id, acao);
            TempData["Sucesso"] = acao == DesfechoLiberacao.Retirar
                ? $"Bloqueio #{id} liberado: {q} un. retiradas do estoque."
                : $"Bloqueio #{id} liberado: {q} un. devolvidas ao estoque.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private BloqueioViewModel Carregar(BloqueioViewModel vm)
    {
        vm.Produtos = _produtos.ConsultarEstoque().Where(p => p.Ativo).ToList();
        return vm;
    }
}
