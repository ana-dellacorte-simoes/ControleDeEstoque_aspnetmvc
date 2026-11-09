namespace ControleEstoque.Exceptions;

/// <summary>Erro de regra de negócio: a mensagem é segura para exibir ao usuário.</summary>
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}

public class EstoqueInsuficienteException : RegraNegocioException
{
    public int Disponivel { get; }

    public EstoqueInsuficienteException(int disponivel)
        : base($"Estoque insuficiente. Quantidade disponível: {disponivel}.")
    {
        Disponivel = disponivel;
    }
}

public class RecursoNaoEncontradoException : RegraNegocioException
{
    public RecursoNaoEncontradoException(string mensagem) : base(mensagem) { }
}
