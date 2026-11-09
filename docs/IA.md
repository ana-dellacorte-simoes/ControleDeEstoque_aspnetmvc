# Uso de Inteligência Artificial

Durante o desenvolvimento do sistema de Controle de Estoque, utilizei **ChatGPT e Claude** como ferramentas de apoio à programação, revisão, testes e documentação.

## 1. Desenvolvimento inicial

O projeto começou com a análise e classificação dos requisitos pelo **Modelo Kano**, elaboração manual do Modelo Entidade-Relacionamento (MER), desenho das telas e posterior prototipação no Figma.

As principais decisões arquiteturais, como a utilização de **C#, ASP.NET Core MVC, SQL Server, ADO.NET, padrão DAO e camada Service**, foram definidas previamente.

Também foram desenvolvidas manualmente a organização das regras de negócio, o tratamento inicial de erros e principalmente a implementação dos DAOs.

A IA foi utilizada posteriormente para **complementar e aperfeiçoar o projeto existente**, preservando sua arquitetura e modelagem.

## 2. Principais prompts utilizados

### Claude — Complementação do sistema

*Resumo do prompt utilizado:*

> Atue como engenheiro de software sênior full-stack e finalize meu projeto de Controle de Estoque, utilizando C#, ASP.NET Core MVC, SQL Server, ADO.NET e DAO, sem ORM.
>
> Preserve a arquitetura Controller → Service → DAO e o código existente. Complete as funcionalidades de produtos, entrada, saída, reservas, bloqueios e liberações individuais, respeitando a modelagem e as regras de disponibilidade.
>
> Garanta validações, proteção contra SQL Injection, CSRF, XSS e overposting, transações, controle de concorrência e testes automatizados.
>
> Organize e comente o código, implemente uma interface clean, moderna e responsiva conforme os desenhos fornecidos e prepare a documentação exigida pelo case.

**Materiais enviados:** ZIP do projeto parcialmente desenvolvido, apostila de referência, enunciado do case e desenhos das telas.

### ChatGPT — Revisão e documentação

*Resumo das orientações utilizadas:*

> Analise o projeto e identifique possíveis falhas de segurança, inconsistências nas regras de negócio e situações excepcionais. Proponha testes adversariais e revise o README e a documentação de IA, respeitando os requisitos do case e a divisão real das etapas de desenvolvimento.

## 3. Onde a IA mais ajudou

- Complementação e padronização de Controllers, Services, Models, ViewModels e Views.
- Replicação dos padrões arquiteturais já implementados para as demais funcionalidades.
- Aperfeiçoamento das validações, do tratamento de erros e dos mecanismos de segurança.
- Refinamento das operações transacionais e inclusão de testes automatizados.
- Desenvolvimento do frontend com base nos protótipos.
- Inclusão de comentários explicativos e apoio na elaboração do README e desta documentação.

## 4. Sugestões e implementações revisadas

**Controle de concorrência:** a implementação inicial utilizava transações com isolamento `SERIALIZABLE` e atualização condicional. Posteriormente, a estratégia foi padronizada com `UPDLOCK`, mantendo a validação de disponibilidade dentro das transações.

**Desativação de produtos:** após a primeira versão complementada pela IA, identifiquei a ausência dessa funcionalidade e solicitei sua implementação. Foi estabelecido que produtos com unidades em estoque, reservas ou bloqueios ativos não poderiam ser desativados. A desativação deveria ser lógica, preservando o histórico. Posteriormente, validei essas regras por meio de testes.

**Motivo do bloqueio:** mantive o preenchimento opcional conforme o desenho inicial, registrando essa escolha como premissa da implementação.

## 5. Limitações identificadas e validação

Uma das principais limitações ocorreu na entrega inicial do Claude: **o código foi gerado sem possibilidade de compilação ou execução no ambiente da IA**, devido à ausência do .NET SDK e do SQL Server.

Por esse motivo, realizei localmente a compilação, os testes automatizados e testes manuais pela interface, verificando as funcionalidades e as regras de negócio.

Também foram realizadas verificações de situações excepcionais, incluindo quantidades inválidas, estoque insuficiente, liberações duplicadas e tentativas de desativação de produtos com saldo ou pendências.

## 6. Análise crítica

A IA foi especialmente útil para acelerar tarefas repetitivas, complementar funcionalidades e apoiar a revisão do sistema.

Entretanto, suas respostas não foram consideradas automaticamente corretas ou completas. Foi necessário analisar as implementações, identificar funcionalidades ausentes, solicitar ajustes e validar os resultados no ambiente local.

A experiência demonstrou que **a IA pode aumentar a produtividade, mas a compreensão das regras de negócio, a tomada de decisões técnicas e a responsabilidade pela validação continuam sendo fundamentais no desenvolvimento de software**.