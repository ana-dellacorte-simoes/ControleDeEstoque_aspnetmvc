using ControleEstoque.DAO;
using ControleEstoque.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ControleEstoque.Services;

/// <summary>Gera o relatório em PDF (somente leitura): situação dos produtos + movimentações do período.</summary>
public class RelatorioService
{
    private readonly ProdutosDAO _produtos;
    private readonly MovimentacoesDAO _movimentacoes;

    static RelatorioService() => QuestPDF.Settings.License = LicenseType.Community;

    public RelatorioService(ProdutosDAO produtos, MovimentacoesDAO movimentacoes)
        => (_produtos, _movimentacoes) = (produtos, movimentacoes);

    private const string Tinta = "#1E1A2B", Verde = "#2A1A55", Brasa = "#B7650F", Papel = "#F1EEF8", Linha = "#DDD7EA", Suave = "#756F88";

    public byte[] GerarPdf(DateTime? de, DateTime? ate, bool incluirInativos)
    {
        // de/ate são datas locais (inclusive). No banco as datas estão em UTC.
        DateTime? deUtc = de.HasValue ? DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Local).ToUniversalTime() : null;
        DateTime? ateUtc = ate.HasValue ? DateTime.SpecifyKind(ate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime() : null;

        var produtos = _produtos.ConsultarEstoque().Where(p => incluirInativos || p.Ativo).ToList();
        var movs = _movimentacoes.ListarPeriodo(deUtc, ateUtc);

        long total = produtos.Where(p => p.Ativo).Sum(p => (long)p.QuantidadeTotal);
        long disp = produtos.Where(p => p.Ativo).Sum(p => (long)p.QuantidadeDisponivel);
        long res = produtos.Where(p => p.Ativo).Sum(p => (long)p.QuantidadeReservada);
        long blo = produtos.Where(p => p.Ativo).Sum(p => (long)p.QuantidadeBloqueada);

        string periodo = (de, ate) switch
        {
            (null, null) => "Todo o histórico",
            ({ } d, null) => $"A partir de {d:dd/MM/yyyy}",
            (null, { } a) => $"Até {a:dd/MM/yyyy}",
            ({ } d, { } a) => $"{d:dd/MM/yyyy} a {a:dd/MM/yyyy}"
        };

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Tinta));

                page.Header().PaddingBottom(14).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Relatório de estoque").FontSize(20).Bold().FontColor(Verde);
                        c.Item().Text($"Movimentações: {periodo}").FontColor(Suave);
                    });
                    row.ConstantItem(150).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("Gerado em").FontSize(8).FontColor(Suave);
                        c.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold();
                    });
                });

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Row(r =>
                    {
                        r.Spacing(8);
                        Kpi(r, "Total", total, Verde);
                        Kpi(r, "Disponível", disp, "#2DBE85");
                        Kpi(r, "Reservado", res, "#4A5FC1");
                        Kpi(r, "Bloqueado", blo, Brasa);
                    });

                    col.Item().Text($"Produtos ({produtos.Count})").FontSize(12).Bold().FontColor(Verde);
                    if (produtos.Count == 0)
                        col.Item().Text("Nenhum produto cadastrado.").FontColor(Suave);
                    else
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(34); c.RelativeColumn(3);
                                c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(1.3f);
                            });
                            t.Header(h =>
                            {
                                Cab(h, "ID", false); Cab(h, "Produto", false); Cab(h, "Total", true);
                                Cab(h, "Disp.", true); Cab(h, "Reserv.", true); Cab(h, "Bloq.", true); Cab(h, "Situação", false);
                            });
                            foreach (var p in produtos)
                            {
                                Cel(t, p.Id.ToString(), false, Suave); Cel(t, p.Nome, false, null, true);
                                Cel(t, p.QuantidadeTotal.ToString("N0"), true); Cel(t, p.QuantidadeDisponivel.ToString("N0"), true, null, true);
                                Cel(t, p.QuantidadeReservada.ToString("N0"), true); Cel(t, p.QuantidadeBloqueada.ToString("N0"), true);
                                var baixo = p.QuantidadeTotal > 0 && p.QuantidadeDisponivel * 10 <= p.QuantidadeTotal;
                                var (txt, cor) = !p.Ativo ? ("Inativo", Suave)
                                    : p.QuantidadeDisponivel == 0 ? ("Sem disponível", "#B3261E")
                                    : baixo ? ("Baixo", Brasa) : ("Normal", "#1F8F63");
                                Cel(t, txt, false, cor, true);
                            }
                        });

                    col.Item().PaddingTop(6).Text($"Movimentações ({movs.Count})").FontSize(12).Bold().FontColor(Verde);
                    if (movs.Count == 0)
                        col.Item().Text("Nenhuma movimentação no período.").FontColor(Suave);
                    else
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.4f); c.RelativeColumn(3); c.RelativeColumn(1.6f); c.RelativeColumn(); c.RelativeColumn(2.4f);
                            });
                            t.Header(h =>
                            {
                                Cab(h, "Data", false); Cab(h, "Produto", false); Cab(h, "Tipo", false);
                                Cab(h, "Qtd.", true); Cab(h, "Referência", false);
                            });
                            foreach (var m in movs)
                            {
                                Cel(t, m.DataCriacao.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), false, Suave);
                                Cel(t, m.ProdutoNome, false);
                                Cel(t, m.Rotulo, false, m.Classe switch { "ok" => "#1F8F63", "danger" => "#B3261E", "info" => "#4A5FC1", "warn" => Brasa, _ => Suave }, true);
                                Cel(t, m.Quantidade.ToString("N0"), true);
                                Cel(t, string.IsNullOrWhiteSpace(m.Referencia) ? "—" : m.Referencia!, false, Suave);
                            }
                        });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(8).FontColor(Suave));
                    x.Span("Página "); x.CurrentPageNumber(); x.Span(" de "); x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Kpi(RowDescriptor r, string rotulo, long valor, string cor)
        => r.RelativeItem().Background(Papel).BorderLeft(3).BorderColor(cor).Padding(8).Column(c =>
        {
            c.Item().Text(rotulo.ToUpperInvariant()).FontSize(7).FontColor(Suave).LetterSpacing(0.08f);
            c.Item().Text(valor.ToString("N0")).FontSize(16).Bold();
        });

    private static void Cab(TableCellDescriptor h, string texto, bool dir)
    {
        var c = h.Cell().Background(Verde).PaddingVertical(5).PaddingHorizontal(6);
        (dir ? c.AlignRight() : c.AlignLeft()).Text(texto).FontSize(8).Bold().FontColor(Colors.White);
    }

    private static void Cel(TableDescriptor t, string texto, bool dir, string? cor = null, bool forte = false)
    {
        var c = t.Cell().BorderBottom(0.5f).BorderColor(Linha).PaddingVertical(4).PaddingHorizontal(6);
        var tx = (dir ? c.AlignRight() : c.AlignLeft()).Text(texto);
        if (cor != null) tx.FontColor(cor);
        if (forte) tx.SemiBold();
    }
}
