namespace ControleEstoque.Models
{
    public class Reserva
    {
        public int IdReserva { get; set; }
        public int IdProduto { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string SolicitacaoId { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public DateTime DataCriacao { get; set; }
        public bool Ativa { get; set; } = true;
        public string? Desfecho { get; set; }
        public DateTime? DataLiberacao { get; set; }
    }
}
