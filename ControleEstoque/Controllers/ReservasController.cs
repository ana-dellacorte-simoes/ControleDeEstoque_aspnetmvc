using System.Globalization;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using ControleEstoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleEstoque.Controllers;

public class ReservasController : Controller
{
    private readonly EstoqueService _estoque;
    private readonly ConsultaService _consulta;
    private readonly ProdutosService _produtos;

    public ReservasController(EstoqueService estoque, ConsultaService consulta, ProdutosService produtos)
        => (_estoque, _consulta, _produtos) = (estoque, consulta, produtos);

    [HttpGet]
    public IActionResult Index(string? q, bool historico = false)
        => View(new ListaReservasViewModel { Busca = q, Historico = historico, Itens = _consulta.ListarReservas(q, historico) });

    [HttpGet]
    public IActionResult Criar(int? produtoId)
        => View(Carregar(new ReservaViewModel { ProdutoId = produtoId ?? 0 }));

    [HttpPost]
    public IActionResult Criar(ReservaViewModel vm)
    {
        Carregar(vm);
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var r = _estoque.Reservar(vm.ProdutoId, vm.Quantidade, vm.SolicitacaoId);
            var nome = vm.Produtos.FirstOrDefault(p => p.Id == vm.ProdutoId)?.Nome ?? $"#{vm.ProdutoId}";
            TempData["Sucesso"] = $"Reserva de \"{nome}\" realizada. ID da reserva: {r.Id} · " +
                $"Data: {r.DataCriacao.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)} · Quantidade: {r.Quantidade}.";
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
            int q = _estoque.LiberarReserva(id, acao);
            TempData["Sucesso"] = acao == DesfechoLiberacao.Retirar
                ? $"Reserva #{id} liberada: {q} un. retiradas do estoque."
                : $"Reserva #{id} liberada: {q} un. devolvidas ao estoque.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private ReservaViewModel Carregar(ReservaViewModel vm)
    {
        vm.Produtos = _produtos.ConsultarEstoque().Where(p => p.Ativo).ToList();
        return vm;
    }
}
