namespace ControleEstoque.Models;

public class EstoqueProduto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int QuantidadeTotal { get; set; }
    public int QuantidadeReservada { get; set; }
    public int QuantidadeBloqueada { get; set; }
    public bool Ativo { get; set; }

    // Disponível = Total − Reservado ativo − Bloqueado ativo
    public int QuantidadeDisponivel =>
        QuantidadeTotal - QuantidadeReservada - QuantidadeBloqueada;
}
