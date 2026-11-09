namespace ControleEstoque.Models;

public class Movimentacao
{
    public int Id { get; set; }
    public int IdProduto { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public string? Referencia { get; set; }
    public DateTime DataCriacao { get; set; }

    public string Rotulo => Tipo switch
    {
        "Entrada" => "Entrada",
        "Saida" => "Saída",
        "Reserva" => "Reserva",
        "Bloqueio" => "Bloqueio",
        "ReservaDevolvida" => "Reserva devolvida",
        "ReservaRetirada" => "Reserva retirada",
        "BloqueioDevolvido" => "Bloqueio devolvido",
        "BloqueioRetirado" => "Bloqueio retirado",
        _ => Tipo
    };

    public string Classe => Tipo switch
    {
        "Entrada" => "ok",
        "Saida" => "danger",
        "Reserva" => "info",
        "Bloqueio" => "warn",
        _ => "muted"
    };
}
