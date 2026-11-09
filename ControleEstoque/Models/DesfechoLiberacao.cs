namespace ControleEstoque.Models;

/// <summary>Destino da quantidade ao liberar uma reserva/bloqueio. 0 é intencionalmente inválido.</summary>
public enum DesfechoLiberacao
{
    Devolver = 1,
    Retirar = 2
}

public record RegistroCriado(int Id, DateTime DataCriacao, int Quantidade);
