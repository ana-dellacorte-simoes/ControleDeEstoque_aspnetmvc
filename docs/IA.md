# Uso de IA

## Prompt principal utilizado
> Atue como engenheiro de software sênior full-stack e finalize meu projeto de Controle de Estoque (C#, ASP.NET Core MVC, SQL Server com ADO.NET + DAO, sem ORM), mantendo Controller → Service → DAO. Implementar produtos, entrada, saída, reservas, bloqueios e liberações individuais; Disponível = Total − Reservado ativo − Bloqueado ativo; segurança (SQL Injection, CSRF, XSS, overposting), transações e concorrência; testes automatizados; interface clean/corporativa (laranja, grafite, azul profundo) seguindo meu desenho; README conforme o case.

(Anexos: ZIP do projeto, apostila, enunciado e desenho manuscrito.)

## Onde a IA mais ajudou
- Estrutura do trabalho a partir do projeto existente (evolução, não reescrita).
- Esqueleto repetitivo: DAOs de listagem, ViewModels, views, CSS responsivo.
- Padrão de concorrência (lock do produto + validação dentro da transação) e casos de teste de concorrência.

## Sugestões que foram ajustadas
- *(Preencha com a sua análise)* Ex.: o `Saida` original usava `SERIALIZABLE` com `UPDATE` condicional; foi substituído por lock de linha (`UPDLOCK`) por reduzir risco de deadlock e unificar o padrão entre todas as operações.
- Motivo do bloqueio: o enunciado do pedido dizia "registrar o motivo", o desenho diz "não obrigatória". Mantive **opcional**.

## Limitações / problemas a verificar
- **O código gerado não foi compilado nem executado** (ambiente sem .NET/SQL Server). Pode haver erros de compilação ou de SQL a corrigir.
- Os testes de integração/concorrência só rodam com `CONTROLE_ESTOQUE_TEST_CONN` definido; precisam ser executados por você.
- Versões de pacotes de teste (xunit, Test SDK) foram escolhidas de memória; ajuste se o restore reclamar.

## Para a entrevista
Seja capaz de explicar: por que disponível é derivado; por que `UPDLOCK` antes de ler os somatórios; como a liberação duplicada é impedida; diferença *devolver* × *retirar*; por que a regra de saldo está no DAO e não no Service; limites da solução (sem autenticação, sem retry de deadlock).
