using ControleEstoque.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControleEstoque.Controllers;

public class RelatoriosController : Controller
{
    private readonly RelatorioService _relatorio;
    public RelatoriosController(RelatorioService relatorio) => _relatorio = relatorio;

    [HttpGet]
    public IActionResult Index() => View();

    // GET sem efeito colateral (somente leitura): seguro mesmo sem token antiforgery.
    [HttpGet]
    public IActionResult Pdf(DateTime? de, DateTime? ate, bool incluirInativos = false)
    {
        if (de.HasValue && ate.HasValue && de > ate)
        {
            TempData["Erro"] = "A data inicial não pode ser maior que a data final.";
            return RedirectToAction(nameof(Index));
        }
        var bytes = _relatorio.GerarPdf(de, ate, incluirInativos);
        return File(bytes, "application/pdf", $"relatorio-estoque-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }
}
