# SRS — Fechamento diário de índices de mercado (Ibovespa)

- **Data:** 2026-10-11
- **Feature:** `MarketData/Indexes` + `Integrations/MarketData/B3/Indexes`
- **Status:** **entregue, exceto a consulta.** O agregado `IndexQuote`, a tabela `index_quotes`, a importação a partir da B3 e a mutation `importIndexQuotes` estão em `main` (§8). **Especificado, não implementado:** a query `indexQuotes` (RF-04, §6). Carga feita: o Ibovespa de 1968 a 2026 (§11).
- **Fonte externa:** endpoint de estatísticas por índice do site da B3 (`sistemaswebb3-listados.b3.com.br/indexStatisticsProxy`), sem chave. **Não é API documentada**: é o proxy que alimenta a página "Estatísticas históricas" de cada índice, o mesmo usado pelo pacote `rb3` (ropensci).
- **Base empírica:** o endpoint foi conferido em **2026-10-11** contra o fechamento publicado pela imprensa (29/10/2018: 83.796,71 pontos) e contra a série 7 do Banco Central e o Yahoo `^BVSP` (diferença de 1 ponto, de arredondamento).

---

## 1. Introdução

### 1.1 Propósito
Guardar o **fechamento diário do Ibovespa oficial**, para que estudos de evento (relatórios de eleição, impeachment, circuit breakers) comparem cada papel com o índice de verdade, e não com um substituto.

O substituto que se usava é o ETF `BOVA11`, e ele erra o índice. Em 08/10/2018 o Ibovespa subiu **4,57%** (82.321,52 → 86.083,92) e o `BOVA11` subiu **5,27%** no mesmo dia: 0,7 ponto percentual de ruído, e mais em dias de estresse. Além disso o `BOVA11` só existe desde 02/12/2008, o que deixa as eleições de 2002 e 2006 sem referência.

### 1.2 Escopo
- Um fechamento por índice e por pregão, em pontos, com duas casas decimais.
- Série **desde 1968** (o que a B3 publica) até o último pregão, **bruta** (ver RN-08).
- Só o **Ibovespa** na primeira versão; o modelo já admite outros índices (ver RN-01).

**Fora de escopo (hoje):**
- Abertura, máxima, mínima e volume do índice: a B3 publica só o fechamento nesse endpoint.
- Composição teórica da carteira e pesos dos papéis.
- Retornos calculados. A tabela guarda o valor; o retorno é derivado na leitura.
- Outras fontes gravadas. O Yahoo e o Banco Central servem de **conferência**, não são persistidos (RN-03).

### 1.3 Definições
| Termo | Significado |
|---|---|
| **Ibovespa** | Índice de retorno total das ações mais negociadas da B3, em pontos. |
| **Fechamento** | Valor do índice ao fim do pregão. |
| **Estudo de evento** | Comparação do retorno de um papel com o do índice em torno de uma data (relatório "efeito eleição"). |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura
```
Domain/MarketData/Indexes
├── MarketIndex                 (enum: Ibovespa)
├── IndexQuoteSource            (enum: B3)
└── IndexQuote                  (aggregate root — fechamento de um índice num pregão)

Application/MarketData/Indexes
├── Import/{IIndexQuoteProvider, IndexQuoteImportItem, IndexQuoteImportResult}
├── Specifications/IndexQuotesByYearSpecification
├── Commands/{ImportIndexQuotesCommand, ImportIndexQuotesCommandHandler}
├── DTOs/Requests/ImportIndexQuotesRequest
└── UseCases/{IImportIndexQuotesUseCase + impl}

Infrastructure/Persistence
├── Configurations/IndexQuoteConfiguration
└── Migrations/AddIndexQuotes

Integrations/MarketData/B3/Indexes
├── Clients/IB3IndexClient             (Refit, payload em base64 no caminho)
├── DTOs/Responses/B3IndexStatisticsResponse
└── Providers/B3IndexQuoteProvider

Api/GraphQL/Mutations/IndexMutations   (importIndexQuotes)

Planejado (não implementado): a query indexQuotes.
```

### 2.2 Por que a B3 e não outra fonte
| Fonte | Cobertura | Problema |
|---|---|---|
| **B3, `GetPortfolioDay`** | 1968 até hoje | Endpoint não documentado (ver §7). **Escolhida.** |
| Banco Central, SGS 7 | 1994 a **30/09/2019** | Descontinuada; valores em pontos inteiros. |
| Yahoo `^BVSP` | 1993 até hoje | Não oficial; 178 dias nulos; `range=max` devolve dados mensais, exige datas explícitas e `User-Agent`. |
| Brapi `^BVSP` | — | Exige token. |
| Derivar do `BOVA11` | 2008 em diante | É exatamente o problema que esta fatia resolve. |

A série 7 do Banco Central teria sido a escolha por ser oficial e estável, mas termina em setembro de 2019: cobriria 2002–2018 e deixaria 2020 (pandemia) e 2022 sem referência.

---

## 3. Requisitos funcionais

| ID | Requisito | Status |
|---|---|---|
| RF-01 | Persistir o fechamento de um índice em um pregão. | Entregue |
| RF-02 | Importar da B3 todos os pregões de um ano para um índice, de forma idempotente. | Entregue |
| RF-03 | Expor a importação por mutation GraphQL. | Entregue |
| RF-04 | Consultar os fechamentos por índice e intervalo de datas. | Não implementado |

## 4. Regras de negócio

- **RN-01 — Identidade.** Uma linha por **índice e pregão**, garantida por índice único em `(market_index, trading_date)`. A chave primária é um **`id` `bigint` comum**, como em `DailyQuote`: o usuário preferiu o `id` simples ao uso da chave natural composta como PK. O custo é um índice a mais, irrelevante aqui: o Ibovespa inteiro tem cerca de 15 mil linhas, contra 5,7 milhões de `daily_quotes`.
- **RN-02 — Fechamento positivo.** `close_value > 0`, no agregado (`index.quote.invalid`) e em `CHECK` no banco (`ck_index_quotes_close_value_positive`), para sobreviver a inserções que contornem o agregado.
- **RN-03 — Uma origem gravada.** A coluna `source` registra de onde veio o valor (hoje só `B3`). Quando houver mais de uma origem, o modelo já sabe distingui-las. Yahoo e Banco Central são usados apenas para conferir.
- **RN-04 — Idempotência.** Reimportar um ano não duplica: o que já existe e é igual é ignorado, o que mudou é atualizado, o que falta é criado.
- **RN-05 — Dia sem pregão não é linha.** A B3 devolve uma matriz dia × mês em que o dia sem pregão vem `null`; esses dias não geram registro.
- **RN-06 — Número em pt-BR.** O valor chega como texto (`"86.083,92"`); o ponto é separador de milhar e a vírgula é o decimal.
- **RN-07 — Ano parcial.** O ano corrente vem até o último pregão. Importar de novo mais tarde completa o ano.
- **RN-08 — Série bruta, com cortes de base.** A tabela guarda o que a B3 publica, **sem ajuste**. Antes de 03/03/1997 o índice foi reescalado por fatores de 10 (e de 100 em 1983) quando se aproximava de 100 mil pontos, então a série tem **11 saltos** de cerca de −90% (−99% em 1983) de um pregão para o seguinte, que **não são quedas de mercado**: 04/10/1983, 03/12/1985, 30/08/1988, 18/04/1989, 15/01/1990, 29/05/1991, 22/01/1992, 27/01/1993, 30/08/1993, 10/02/1994 e 03/03/1997. Uma consulta por variação diária acima de 35% devolve 12 linhas; a outra é a alta de 36,1% em 04/02/1991, que não tem fator de 10 e não foi investigada. **Retornos que atravessam um corte são inválidos**; de 03/03/1997 em diante a série é contínua e dispensa ajuste. Não há descontinuidade na virada do Real, em 01/07/1994. Os cortes ficam registrados aqui, não corrigidos na tabela, para a tabela continuar sendo a fonte oficial.

## 5. Importação

`GET https://sistemaswebb3-listados.b3.com.br/indexStatisticsProxy/IndexCall/GetPortfolioDay/{base64}`

O trecho `{base64}` é o JSON `{"language":"pt-br","index":"IBOV","year":"2018"}` codificado em base64. A resposta traz:

- `results[]`: uma entrada por dia do mês (`day`, de 1 a 31), com `rateValue1` a `rateValue12` (uma coluna por mês, texto pt-BR ou `null`);
- `min` e `max`: mínimo e máximo de cada mês, que **não** são fechamentos e devem ser ignorados.

Um ano por chamada. O mapeamento do enum `MarketIndex` para o código da B3 (`Ibovespa` → `IBOV`) fica no provider, na camada anticorrupção.

## 6. Superfície GraphQL

- **Entregue:** `importIndexQuotes(input: { index: IBOVESPA, year: 2018 })` devolve `rowsRead`, `quotesCreated` e `quotesUpdated`. Não há `fileFound` como em `importDailyQuotes`: aqui um ano sem dados é uma lista vazia com HTTP 200 (a B3 devolve `results: null`), e vira zero linhas, sem erro. Erro de domínio: `index.year.invalid` para ano anterior a 1968. Hoje a mutation não exige autenticação, como `importDailyQuotes`.
- **Planejado:** `indexQuotes`, leitura Estilo B (`IQueryable` com `where`/`order`), por ser uma lista de dados públicos moldada pelo cliente. Exige expor `IndexQuotes` em `IApplicationDbContext` e um `ObjectType` curado.

O contrato é documentado em PT-BR no XML, como manda a regra do repositório; o teste funcional confere que as descrições chegam ao schema.

## 7. Riscos

- **Endpoint sem contrato.** É o proxy interno do site; a B3 pode mudar caminho, formato ou bloquear. Mitigação: um teste de contrato externo no workflow semanal `external-contracts.yml`, e não no CI de cada commit, para que uma queda da B3 não reprove um commit correto.
- **Verificação de certificado.** O `rb3` desliga a verificação de SSL nesse endpoint. Aqui não foi necessário: `curl` e Python funcionaram com a verificação padrão em 2026-10-11. Se isso mudar, é um sintoma a investigar, não um motivo para desligar a verificação.
- **Pontos inteiros no Banco Central.** Se um dia for preciso cruzar com a série 7, a comparação tem tolerância de 1 ponto.

## 8. Rastreabilidade

| Requisito / regra | Commit |
|---|---|
| RF-01, RN-02 (agregado, erro `index.quote.invalid`) | `106cec1` |
| RF-01, RN-01, RN-02 (tabela, índice único, CHECK) | `85c9361` |
| RF-02, RN-04, `index.year.invalid` (comando, handler, caso de uso, porta) | `449e55f` |
| RF-02, RN-03, RN-05, RN-06 (provider da B3) | `08a28ec` |
| RF-03 (mutation) | `661cfb4` |
| RF-04, RN-07 | RN-07 vale desde `449e55f` (reimportar completa o ano); RF-04 pendente |

## 9. Cobertura de testes

- `IndexQuoteTests` (domínio): criação, fechamento não positivo, atualização.
- `DomainErrorCatalogTests`: inclui `index.quote.invalid` e `index.year.invalid`.
- `IndexQuotePersistenceTests` e `ImportIndexQuotesHandlerTests` (Postgres real, Testcontainers): ida e volta com duas casas, índice único, `CHECK`, upsert real (insere, atualiza e pula) e a specification que não toca em outros anos.
- `ImportIndexQuotesCommandHandlerTests` e `ImportIndexQuotesUseCaseTests` (aplicação): criação, atualização, idêntico, ano vazio, ano anterior a 1968.
- `B3IndexQuoteProviderTests` (integrações): matriz dia × mês em pt-BR, ordenação, descarte de dia impossível, valor não positivo e texto não numérico, ano sem dados, payload em base64 e propagação de falha de transporte.
- `ImportIndexQuotesMutationTests` (funcional): contadores, idempotência, ano vazio, erro de domínio e documentação no schema.

## 10. Próximos passos

1. Query `indexQuotes` (RF-04).
2. Decidir como tratar os cortes de base (RN-08) para quem calcula retorno antes de 1997: uma tabela pequena com a data e o fator de cada corte, ou um ajuste na leitura. Hoje nada disso é necessário de 03/03/1997 em diante.
3. Teste de contrato externo semanal contra o endpoint da B3 (§7).
4. Refazer os relatórios de evento com o índice real em vez do `BOVA11`.

## 11. Primeira carga (2026-10-11)

Importados os anos de 2000 a 2026 pela mutation: **6.638 pregões**, de 03/01/2000 a 09/10/2026. A validação cruzada prevista (o número de pregões do índice em um ano deve bater com os dias distintos de `daily_quotes` do mercado à vista) **fechou sem nenhuma diferença**: todos os 27 anos coincidem, e não há pregão de um lado sem o outro. Conferidos pontos de controle: 05/10/2018 = 82.321,52; 08/10/2018 = 86.083,92; 29/10/2018 = 83.796,71.

Na sequência foram importados os anos de **1968 a 1999**, e a tabela passou a ter **14.496 pregões**, de 02/01/1968 a 09/10/2026. A mesma validação cruzada fechou para 1994 (só de julho em diante, 124 pregões), 1995, 1996, 1997, 1998 e 1999. A carga fez aparecer os cortes de base descritos em RN-08.
