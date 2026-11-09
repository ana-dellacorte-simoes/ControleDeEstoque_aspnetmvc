using System.Data;
using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO;

internal static class SqlHelper
{
    /// <summary>Monta o padrão para LIKE escapando curingas (%, _, [ e \), usado com ESCAPE '\'.</summary>
    public static object PadraoLike(string? busca)
    {
        if (string.IsNullOrWhiteSpace(busca)) return DBNull.Value;
        var t = busca.Trim();
        if (t.Length > 100) t = t[..100];
        t = t.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        return $"%{t}%";
    }

    public static SqlParameter Int(this SqlCommand c, string nome, int valor)
        => c.Parameters.Add(nome, SqlDbType.Int).Also(p => p.Value = valor);

    public static SqlParameter Varchar(this SqlCommand c, string nome, int tamanho, object? valor)
        => c.Parameters.Add(nome, SqlDbType.VarChar, tamanho).Also(p => p.Value = valor ?? DBNull.Value);

    private static T Also<T>(this T obj, Action<T> acao) { acao(obj); return obj; }
}
