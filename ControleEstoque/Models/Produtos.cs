namespace ControleEstoque.Models
{
    public class Produtos
    {
        public int IdProduto { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeTotal { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
