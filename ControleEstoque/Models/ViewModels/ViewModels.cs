using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ControleEstoque.Models;

// ViewModels com apenas os campos aceitos do formulário => evita overposting.

public class ProdutoCadastroViewModel
{
    [Required(ErrorMessage = "Informe o nome do produto.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Range(0, 1_000_000, ErrorMessage = "A quantidade inicial deve estar entre 0 e 1.000.000.")]
    [Display(Name = "Quantidade inicial (disponível)")]
    public int QuantidadeInicial { get; set; }
}

public class MovimentoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um produto.")]
    public int ProdutoId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Informe uma quantidade entre 1 e 1.000.000.")]
    public int Quantidade { get; set; }

    [BindNever, ValidateNever] public string Tipo { get; set; } = "Entrada";
    [BindNever, ValidateNever] public List<EstoqueProduto> Produtos { get; set; } = new();
}

public class ReservaViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um produto.")]
    public int ProdutoId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Informe uma quantidade entre 1 e 1.000.000.")]
    public int Quantidade { get; set; }

    [Required(ErrorMessage = "Informe a solicitação/pedido.")]
    [StringLength(50, ErrorMessage = "Máximo de 50 caracteres.")]
    public string SolicitacaoId { get; set; } = string.Empty;

    [BindNever, ValidateNever] public List<EstoqueProduto> Produtos { get; set; } = new();
}

public class BloqueioViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um produto.")]
    public int ProdutoId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Informe uma quantidade entre 1 e 1.000.000.")]
    public int Quantidade { get; set; }

    [StringLength(200, ErrorMessage = "Máximo de 200 caracteres.")]
    public string? Motivo { get; set; }

    [BindNever, ValidateNever] public List<EstoqueProduto> Produtos { get; set; } = new();
}

public class ListaEstoqueViewModel
{
    public string? Busca { get; set; }
    public List<EstoqueProduto> Itens { get; set; } = new();
}

public class ListaReservasViewModel
{
    public string? Busca { get; set; }
    public bool Historico { get; set; }
    public List<Reserva> Itens { get; set; } = new();
}

public class ListaBloqueiosViewModel
{
    public string? Busca { get; set; }
    public bool Historico { get; set; }
    public List<Bloqueio> Itens { get; set; } = new();
}

public class DashboardViewModel
{
    public int TotalProdutos { get; set; }
    public long Total { get; set; }
    public long Disponivel { get; set; }
    public long Reservado { get; set; }
    public long Bloqueado { get; set; }
    public List<EstoqueProduto> Atencao { get; set; } = new();
    public List<Movimentacao> Recentes { get; set; } = new();

    public double Pct(long valor) => Total <= 0 ? 0 : Math.Round(100.0 * valor / Total, 1);
}
