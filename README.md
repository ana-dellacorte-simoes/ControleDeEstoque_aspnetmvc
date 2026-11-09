# Controle de Estoque

Sistema web desenvolvido em **C# / ASP.NET Core MVC**, com **SQL Server e ADO.NET**, para controlar entradas, saídas, reservas e bloqueios de produtos, preservando a consistência das quantidades.

## Funcionalidades

- Cadastro, consulta e desativação lógica de produtos.
- Entrada e saída de unidades, condicionando a saída ao saldo disponível.
- Reservas vinculadas a solicitações e bloqueios independentes, com identificadores próprios.
- Liberação individual de reservas e bloqueios, com opção de devolver à disponibilidade ou retirar do estoque, preservando o histórico.
- Dashboard, consulta de saldos e histórico de movimentações.

**Disponível = Total − Reservado ativo − Bloqueado ativo.**

## Arquitetura e decisões técnicas

A aplicação utiliza **ASP.NET Core MVC** e separação de responsabilidades:

`Views (Razor) → Controllers → Services → DAOs → SQL Server`

- **Controllers:** recebem as requisições e coordenam o fluxo HTTP.
- **Services:** concentram as regras de negócio e validações.
- **DAOs:** executam SQL parametrizado por ADO.NET e controlam as operações transacionais.
- **Models/ViewModels:** representam as entidades e os dados de entrada e apresentação.

A escolha por **DAO e SQL manual**, sem ORM, buscou tornar explícitos o acesso aos dados e o controle das transações. As operações críticas utilizam bloqueios transacionais sobre o produto para reduzir riscos de inconsistência em acessos concorrentes. Há validação no servidor, proteção antifalsificação (CSRF), codificação de saída pelo Razor e tratamento de erros.

## Processo de desenvolvimento e uso de IA

O trabalho começou com **análise dos requisitos e priorização pelo modelo Kano**, elaboração manual do **MER**, desenho dos fluxos e telas e posterior prototipação no **Figma**.

As **decisões arquiteturais**, a escolha de MVC, ADO.NET e DAO, a organização das regras de negócio, o tratamento inicial de erros e a implementação **principalmente dos DAOs** foram desenvolvidos manualmente.

Em seguida, o projeto parcial, a modelagem, os protótipos e o enunciado foram fornecidos ao **Claude**, com a orientação de **aperfeiçoar e completar a solução existente**, preservando suas decisões. A IA auxiliou especialmente na expansão das demais camadas (Controllers, Services, Models/ViewModels e Views), na interface, nos comentários, no aprimoramento das validações e na elaboração de testes. O **ChatGPT** também apoiou revisões, explicações e a redação deste **README**.

O código foi revisado e a aplicação foi validada localmente por **testes manuais de interface e testes automatizados**. O uso de IA não substituiu a análise crítica das soluções. Os prompts relevantes, ajustes e limitações identificadas são descritos em [docs/IA.md](docs/IA.md).

## Como executar

**Pré-requisitos:** .NET SDK 10 e SQL Server.

Na raiz do repositório:

1. Execute `Database/script.sql` no SQL Server (o arquivo `Database/seed.sql` é opcional).
2. Configure `ConnectionStrings:DefaultConnection` por *user-secrets* ou variável de ambiente. Não publique credenciais.
3. Execute:

```bash
dotnet restore
dotnet build
dotnet run --project ControleEstoque
```

Acesse o endereço exibido no terminal.

### Testes

```bash
dotnet test ControleEstoque.Tests
```

Os testes de integração e concorrência exigem a variável `CONTROLE_ESTOQUE_TEST_CONN`, apontando para um **banco de testes isolado**, previamente preparado com o script SQL. Sem essa variável, esses testes são ignorados (*Skipped*), não aprovados.

## Premissas e limitações

- Quantidades movimentadas devem ser positivas; saídas, reservas e bloqueios não podem ultrapassar o saldo disponível.
- Cada reserva ou bloqueio é individual e só pode ser liberado uma vez.
- A desativação lógica preserva o histórico e é restrita a produtos sem saldo e sem pendências ativas.
- A solução foi dimensionada para o escopo do case: não contempla autenticação, autorização por perfil nem infraestrutura de produção.
- O motivo do bloqueio é opcional na implementação atual; essa decisão pode ser revista conforme a regra de negócio desejada.

## Cinco aspectos essenciais de engenharia

1. **Consistência:** impedir saldos inválidos, inclusive sob concorrência.
2. **Segurança:** validar entradas, parametrizar SQL e proteger requisições.
3. **Testabilidade:** automatizar cenários normais, inválidos e concorrentes.
4. **Manutenibilidade:** separar responsabilidades e manter código compreensível.
5. **Observabilidade e tratamento de falhas:** registrar erros úteis e fornecer mensagens adequadas ao usuário.

## Evolução para produção

Implementaria autenticação e autorização, auditoria estruturada, monitoramento, gestão segura de segredos, testes em pipeline de CI/CD, migrações versionadas e testes adicionais de carga e concorrência. Também revisaria regras de negócio opcionais e a estratégia de recuperação de falhas.

---

**Documentação complementar:** [Uso de IA](docs/IA.md) · `Database/script.sql` · `Database/seed.sql`.
