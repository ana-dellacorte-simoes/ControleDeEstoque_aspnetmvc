namespace ControleEstoque.Models
{
    public class Bloqueio
    {
        public int IdBloqueio { get; set; }
        public int IdProduto { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public bool Ativo { get; set; } = true;
        public string? Desfecho { get; set; }
        public DateTime? DataLiberacao { get; set; }
    }
}
