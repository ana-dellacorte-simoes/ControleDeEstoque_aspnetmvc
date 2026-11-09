using ControleEstoque.DAO;
using ControleEstoque.Exceptions;
using ControleEstoque.Models;
using Xunit;

namespace ControleEstoque.Tests;

/// <summary>Regras de estoque e concorrência contra SQL Server real.</summary>
public class EstoqueIntegracaoTests
{
    private readonly ProdutosDAO _produtos;
    private readonly EstoqueDAO _estoque;

    public EstoqueIntegracaoTests()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Banco.Var)))
        {   // construtor roda mesmo com Skip; evita criar conexão sem string
            _produtos = null!; _estoque = null!; return;
        }
        var c = Banco.Conexao();
        _produtos = new ProdutosDAO(c);
        _estoque = new EstoqueDAO(c);
    }

    private EstoqueProduto Situacao(int id) => _produtos.ObterEstoque(id)!;

    [FactSqlServer]
    public void Entrada_e_saida_atualizam_total()
    {
        int id = Banco.NovoProduto(_produtos, 10);
        _estoque.Entrada(id, 5);
        _estoque.Saida(id, 3);
        var s = Situacao(id);
        Assert.Equal(12, s.QuantidadeTotal);
        Assert.Equal(12, s.QuantidadeDisponivel);
    }

    [FactSqlServer]
    public void Saida_reserva_e_bloqueio_acima_do_disponivel_sao_recusados_sem_alterar_estado()
    {
        int id = Banco.NovoProduto(_produtos, 10);
        _estoque.Reservar(id, 4, "PED-A");
        _estoque.Bloquear(id, 3, "teste");        // disponível = 3

        Assert.Throws<EstoqueInsuficienteException>(() => _estoque.Saida(id, 4));
        Assert.Throws<EstoqueInsuficienteException>(() => _estoque.Reservar(id, 4, "PED-B"));
        Assert.Throws<EstoqueInsuficienteException>(() => _estoque.Bloquear(id, 4, ""));

        var s = Situacao(id);
        Assert.Equal((10, 4, 3, 3), (s.QuantidadeTotal, s.QuantidadeReservada, s.QuantidadeBloqueada, s.QuantidadeDisponivel));
    }

    [FactSqlServer]
    public void Produto_inexistente_gera_erro_de_negocio()
        => Assert.Throws<RecursoNaoEncontradoException>(() => _estoque.Reservar(int.MaxValue, 1, "X"));

    [FactSqlServer]
    public void Liberar_devolvendo_restaura_disponivel_e_preserva_historico()
    {
        int id = Banco.NovoProduto(_produtos, 10);
        var r = _estoque.Reservar(id, 6, "PED-1");
        Assert.Equal(4, Situacao(id).QuantidadeDisponivel);

        Assert.Equal(6, _estoque.LiberarReserva(r.Id, DesfechoLiberacao.Devolver));
        var s = Situacao(id);
        Assert.Equal((10, 0, 10), (s.QuantidadeTotal, s.QuantidadeReservada, s.QuantidadeDisponivel));

        var historico = new ReservasDAO(Banco.Conexao()).Listar("PED-1", incluirHistorico: true);
        var linha = Assert.Single(historico.Where(x => x.IdReserva == r.Id));
        Assert.False(linha.Ativa);
        Assert.Equal("Devolvida", linha.Desfecho);
    }

    [FactSqlServer]
    public void Liberar_retirando_reduz_o_total()
    {
        int id = Banco.NovoProduto(_produtos, 10);
        var b = _estoque.Bloquear(id, 4, "avaria");
        _estoque.LiberarBloqueio(b.Id, DesfechoLiberacao.Retirar);
        var s = Situacao(id);
        Assert.Equal((6, 0, 0, 6), (s.QuantidadeTotal, s.QuantidadeReservada, s.QuantidadeBloqueada, s.QuantidadeDisponivel));
    }

    [FactSqlServer]
    public void Liberacao_duplicada_e_recusada()
    {
        int id = Banco.NovoProduto(_produtos, 5);
        var r = _estoque.Reservar(id, 2, "PED-2");
        _estoque.LiberarReserva(r.Id, DesfechoLiberacao.Devolver);
        Assert.Throws<RegraNegocioException>(() => _estoque.LiberarReserva(r.Id, DesfechoLiberacao.Retirar));
        Assert.Equal(5, Situacao(id).QuantidadeTotal);
    }

    [FactSqlServer]
    public void Varias_reservas_e_bloqueios_independentes_no_mesmo_produto()
    {
        int id = Banco.NovoProduto(_produtos, 20);
        var r1 = _estoque.Reservar(id, 3, "A"); var r2 = _estoque.Reservar(id, 4, "B");
        var b1 = _estoque.Bloquear(id, 2, "x");  var b2 = _estoque.Bloquear(id, 1, "y");
        Assert.Equal(10, Situacao(id).QuantidadeDisponivel);

        _estoque.LiberarReserva(r1.Id, DesfechoLiberacao.Devolver);
        _estoque.LiberarBloqueio(b2.Id, DesfechoLiberacao.Devolver);
        var s = Situacao(id);
        Assert.Equal((4, 2, 14), (s.QuantidadeReservada, s.QuantidadeBloqueada, s.QuantidadeDisponivel));
    }

    // ------------------------------------------------------------ concorrência
    private static async Task<(int ok, int insuficiente)> Disparar(int n, Func<int, Action> fabrica)
    {
        int ok = 0, insuf = 0;
        using var largada = new ManualResetEventSlim(false);
        var tarefas = Enumerable.Range(0, n).Select(i => Task.Run(() =>
        {
            var acao = fabrica(i);
            largada.Wait();
            try { acao(); Interlocked.Increment(ref ok); }
            catch (EstoqueInsuficienteException) { Interlocked.Increment(ref insuf); }
        })).ToArray();
        await Task.Delay(100);
        largada.Set();
        await Task.WhenAll(tarefas);
        return (ok, insuf);
    }

    [FactSqlServer]
    public async Task Reservas_concorrentes_nunca_ultrapassam_o_disponivel()
    {
        int id = Banco.NovoProduto(_produtos, 10);
        var (ok, insuf) = await Disparar(40, i => () => _estoque.Reservar(id, 1, $"PED-{i}"));

        Assert.Equal(10, ok);
        Assert.Equal(30, insuf);
        var s = Situacao(id);
        Assert.Equal((10, 10, 0), (s.QuantidadeTotal, s.QuantidadeReservada, s.QuantidadeDisponivel));
    }

    [FactSqlServer]
    public async Task Saidas_reservas_e_bloqueios_simultaneos_mantem_invariantes()
    {
        int id = Banco.NovoProduto(_produtos, 12);
        var (ok, insuf) = await Disparar(60, i => (i % 3) switch
        {
            0 => () => _estoque.Saida(id, 1),
            1 => () => _estoque.Reservar(id, 1, $"P{i}"),
            _ => () => _estoque.Bloquear(id, 1, "c")
        });

        Assert.Equal(12, ok);               // 12 unidades => exatamente 12 operações vencem
        Assert.Equal(48, insuf);
        var s = Situacao(id);
        Assert.True(s.QuantidadeDisponivel >= 0);
        Assert.Equal(0, s.QuantidadeDisponivel);      // tudo foi consumido
        Assert.True(s.QuantidadeTotal >= s.QuantidadeReservada + s.QuantidadeBloqueada);
    }

    [FactSqlServer]
    public void Desativar_e_recusado_com_disponivel_reserva_ou_bloqueio()
    {
        int idComSaldo = Banco.NovoProduto(_produtos, 5);
        Assert.Throws<RegraNegocioException>(() => _produtos.Desativar(idComSaldo));

        int idComReserva = Banco.NovoProduto(_produtos, 3);
        _estoque.Reservar(idComReserva, 3, "PED-D");
        Assert.Throws<RegraNegocioException>(() => _produtos.Desativar(idComReserva));

        int idComBloqueio = Banco.NovoProduto(_produtos, 2);
        _estoque.Bloquear(idComBloqueio, 2, "insp");
        Assert.Throws<RegraNegocioException>(() => _produtos.Desativar(idComBloqueio));

        Assert.True(Situacao(idComSaldo).Ativo);
        Assert.True(Situacao(idComReserva).Ativo);
        Assert.True(Situacao(idComBloqueio).Ativo);
    }

    [FactSqlServer]
    public void Desativar_funciona_quando_zerado_e_sem_pendencias_e_e_idempotente_na_recusa()
    {
        int id = Banco.NovoProduto(_produtos, 4);
        var r = _estoque.Reservar(id, 4, "PED-E");
        _estoque.LiberarReserva(r.Id, DesfechoLiberacao.Retirar);   // total volta a 0, sem pendências

        _produtos.Desativar(id);
        Assert.False(Situacao(id).Ativo);

        // já inativo: nova tentativa é recusada, não lança exceção genérica nem reativa
        Assert.Throws<RegraNegocioException>(() => _produtos.Desativar(id));
        Assert.False(Situacao(id).Ativo);
    }

    [FactSqlServer]
    public void Desativar_e_reservar_concorrentes_nunca_deixam_reserva_orfa_em_produto_inativo()
    {
        int id = Banco.NovoProduto(_produtos, 1);
        _estoque.Saida(id, 1); // total = 0, sem pendências: elegível para desativação

        int ok = 0, recusado = 0;
        using var largada = new ManualResetEventSlim(false);
        var t1 = Task.Run(() => { largada.Wait(); try { _produtos.Desativar(id); Interlocked.Increment(ref ok); } catch (RegraNegocioException) { Interlocked.Increment(ref recusado); } });
        var t2 = Task.Run(() => { largada.Wait(); try { _estoque.Reservar(id, 1, "PED-F"); Interlocked.Increment(ref ok); } catch (Exception) { Interlocked.Increment(ref recusado); } });
        Thread.Sleep(50); largada.Set();
        Task.WaitAll(t1, t2);

        var s = Situacao(id);
        // ou o produto ficou inativo e sem reserva, ou ficou ativo com a reserva — nunca as duas coisas ao mesmo tempo
        Assert.True((!s.Ativo && s.QuantidadeReservada == 0) || (s.Ativo && s.QuantidadeReservada == 1));
    }

    [FactSqlServer]
    public async Task Liberacao_simultanea_da_mesma_reserva_so_funciona_uma_vez()
    {
        int id = Banco.NovoProduto(_produtos, 10);
        var r = _estoque.Reservar(id, 5, "PED-C");

        int sucesso = 0, recusadas = 0;
        await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() =>
        {
            try { _estoque.LiberarReserva(r.Id, DesfechoLiberacao.Retirar); Interlocked.Increment(ref sucesso); }
            catch (RegraNegocioException) { Interlocked.Increment(ref recusadas); }
        })));

        Assert.Equal(1, sucesso);
        Assert.Equal(11, recusadas);
        Assert.Equal(5, Situacao(id).QuantidadeTotal);   // retirou 5 apenas UMA vez
    }
}
