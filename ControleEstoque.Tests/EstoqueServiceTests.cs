using ControleEstoque.DAO;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using ControleEstoque.Services;
using Xunit;

namespace ControleEstoque.Tests;

/// <summary>Testes unitários da validação do Service (DAO falso, sem banco).</summary>
public class EstoqueServiceTests
{
    private sealed class DaoFalso : IEstoqueDAO
    {
        public List<string> Chamadas { get; } = new();
        public string? UltimaSolicitacao, UltimoMotivo;

        public void Entrada(int p, int q) => Chamadas.Add($"Entrada:{p}:{q}");
        public void Saida(int p, int q) => Chamadas.Add($"Saida:{p}:{q}");
        public RegistroCriado Reservar(int p, int q, string s) { Chamadas.Add("Reservar"); UltimaSolicitacao = s; return new(1, DateTime.UtcNow, q); }
        public RegistroCriado Bloquear(int p, int q, string m) { Chamadas.Add("Bloquear"); UltimoMotivo = m; return new(1, DateTime.UtcNow, q); }
        public int LiberarReserva(int id, DesfechoLiberacao d) { Chamadas.Add("LiberarReserva"); return 1; }
        public int LiberarBloqueio(int id, DesfechoLiberacao d) { Chamadas.Add("LiberarBloqueio"); return 1; }
    }

    private static (EstoqueService svc, DaoFalso dao) Criar() { var d = new DaoFalso(); return (new EstoqueService(d), d); }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    [InlineData(EstoqueService.QuantidadeMaxima + 1)]
    public void Quantidade_invalida_e_rejeitada_sem_chamar_o_DAO(int q)
    {
        var (svc, dao) = Criar();
        Assert.Throws<RegraNegocioException>(() => svc.Entrada(1, q));
        Assert.Throws<RegraNegocioException>(() => svc.Saida(1, q));
        Assert.Throws<RegraNegocioException>(() => svc.Reservar(1, q, "PED-1"));
        Assert.Throws<RegraNegocioException>(() => svc.Bloquear(1, q, "x"));
        Assert.Empty(dao.Chamadas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Produto_invalido_e_rejeitado(int id)
    {
        var (svc, dao) = Criar();
        Assert.Throws<RegraNegocioException>(() => svc.Entrada(id, 1));
        Assert.Empty(dao.Chamadas);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reserva_exige_solicitacao(string? s)
    {
        var (svc, dao) = Criar();
        Assert.Throws<RegraNegocioException>(() => svc.Reservar(1, 1, s));
        Assert.Empty(dao.Chamadas);
    }

    [Fact]
    public void Solicitacao_acima_de_50_caracteres_e_rejeitada()
    {
        var (svc, _) = Criar();
        Assert.Throws<RegraNegocioException>(() => svc.Reservar(1, 1, new string('a', 51)));
    }

    [Fact]
    public void Solicitacao_e_aparada_antes_de_chegar_ao_DAO()
    {
        var (svc, dao) = Criar();
        svc.Reservar(1, 2, "  PED-9  ");
        Assert.Equal("PED-9", dao.UltimaSolicitacao);
    }

    [Fact]
    public void Motivo_e_opcional_e_limitado_a_200()
    {
        var (svc, dao) = Criar();
        svc.Bloquear(1, 1, null);
        Assert.Equal(string.Empty, dao.UltimoMotivo);
        Assert.Throws<RegraNegocioException>(() => svc.Bloquear(1, 1, new string('x', 201)));
    }

    [Fact]
    public void Desfecho_fora_do_enum_e_rejeitado()
    {
        var (svc, dao) = Criar();
        Assert.Throws<RegraNegocioException>(() => svc.LiberarReserva(1, (DesfechoLiberacao)0));
        Assert.Throws<RegraNegocioException>(() => svc.LiberarBloqueio(1, (DesfechoLiberacao)99));
        Assert.Empty(dao.Chamadas);
    }

    [Fact]
    public void Operacoes_validas_chegam_ao_DAO()
    {
        var (svc, dao) = Criar();
        svc.Entrada(1, 5); svc.Saida(1, 2); svc.Reservar(1, 1, "P"); svc.Bloquear(1, 1, "m");
        svc.LiberarReserva(1, DesfechoLiberacao.Devolver); svc.LiberarBloqueio(1, DesfechoLiberacao.Retirar);
        Assert.Equal(6, dao.Chamadas.Count);
    }

    [Fact]
    public void Disponivel_e_Total_menos_Reservado_menos_Bloqueado()
    {
        var e = new EstoqueProduto { QuantidadeTotal = 30, QuantidadeReservada = 10, QuantidadeBloqueada = 10 };
        Assert.Equal(10, e.QuantidadeDisponivel);
    }
}
