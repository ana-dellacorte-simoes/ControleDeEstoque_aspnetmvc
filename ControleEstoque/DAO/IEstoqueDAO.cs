using ControleEstoque.Models;

namespace ControleEstoque.DAO;

/// <summary>Operações que alteram o estoque. Cada método é uma transação atômica.</summary>
public interface IEstoqueDAO
{
    void Entrada(int produtoId, int quantidade);
    void Saida(int produtoId, int quantidade);
    RegistroCriado Reservar(int produtoId, int quantidade, string solicitacaoId);
    RegistroCriado Bloquear(int produtoId, int quantidade, string motivo);
    /// <summary>Retorna a quantidade liberada.</summary>
    int LiberarReserva(int reservaId, DesfechoLiberacao desfecho);
    int LiberarBloqueio(int bloqueioId, DesfechoLiberacao desfecho);
}
