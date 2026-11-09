using System.Globalization;
using ControleEstoque.DAO;
using ControleEstoque.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// CSRF: valida token em TODO POST/PUT/DELETE por padrão (não depende de lembrar o atributo).
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddAntiforgery(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.Cookie.SameSite = SameSiteMode.Strict;
});

// DAO -> Service
builder.Services.AddScoped<ConexaoBD>();
builder.Services.AddScoped<ProdutosDAO>();
builder.Services.AddScoped<ReservasDAO>();
builder.Services.AddScoped<BloqueiosDAO>();
builder.Services.AddScoped<MovimentacoesDAO>();
builder.Services.AddScoped<IEstoqueDAO, EstoqueDAO>();
builder.Services.AddScoped<ProdutosService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<ConsultaService>();
builder.Services.AddScoped<RelatorioService>();

var app = builder.Build();

var cultura = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura }
});

// Nunca expõe stack trace / mensagens de exceção ao usuário (detalhes só em log).
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error");
if (!app.Environment.IsDevelopment())
    app.UseHsts();

// Cabeçalhos de segurança + CSP restritiva (sem scripts/estilos inline, sem CDNs).
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    h["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Necessário para testes de integração (WebApplicationFactory), se adicionados no futuro.
public partial class Program { }
