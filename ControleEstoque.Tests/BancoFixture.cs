using ControleEstoque.DAO;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ControleEstoque.Tests;

/// <summary>
/// Testes de integração exigem um SQL Server com o Database/script.sql aplicado.
/// Defina CONTROLE_ESTOQUE_TEST_CONN (ex.: "Server=localhost;Database=ControleEstoqueDB;Trusted_Connection=True;TrustServerCertificate=True;").
/// Sem a variável, os testes são marcados como Skipped (não falham, não fingem passar).
/// </summary>
public static class Banco
{
    public const string Var = "CONTROLE_ESTOQUE_TEST_CONN";

    public static ConexaoBD Conexao()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable(Var)
        }).Build();
        return new ConexaoBD(cfg);
    }

    /// <summary>Cada teste usa produto novo (nome único) => testes independentes, sem limpar o banco.</summary>
    public static int NovoProduto(ProdutosDAO dao, int quantidade)
        => dao.Inserir(new ControleEstoque.Models.Produtos { Nome = "T-" + Guid.NewGuid().ToString("N")[..20], QuantidadeTotal = quantidade });
}

public sealed class FactSqlServerAttribute : FactAttribute
{
    public FactSqlServerAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Banco.Var)))
            Skip = $"Defina {Banco.Var} para rodar os testes de integração com SQL Server.";
    }
}
