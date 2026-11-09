using System.Diagnostics;
using ControleEstoque.Models;
using ControleEstoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleEstoque.Controllers;

public class HomeController : Controller
{
    private readonly ConsultaService _consulta;

    public HomeController(ConsultaService consulta) => _consulta = consulta;

    [HttpGet]
    public IActionResult Index() => View(_consulta.ObterDashboard());

    // Sem [HttpGet]: o re-execute de status code mantém o verbo original (ex.: POST com token inválido).
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Apenas um identificador de correlação é exibido; detalhes técnicos ficam só nos logs.
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
