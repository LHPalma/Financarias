# SRS — Fechamento diário de índices de mercado (Ibovespa)

- **Data:** 2026-10-11
- **Feature:** `MarketData/Indexes` + `Integrations/MarketData/B3/Indexes`
- **Status:** **parcial.** Entregue: o agregado `IndexQuote` e a tabela `index_quotes` (commits `106cec1` e `85c9361`). **Especificado, não implementado:** a importação a partir da B3 e a superfície GraphQL (§5 e §6).
- **Fonte externa:** endpoint de estatísticas por índice do site da B3 (`sistemaswebb3-listados.b3.com.br/indexStatisticsProxy`), sem chave. **Não é API documentada**: é o proxy que alimenta a página "Estatísticas históricas" de cada índice, o mesmo usado pelo pacote `rb3` (ropensci).
- **Base empírica:** o endpoint foi conferido em **2026-10-11** contra o fechamento publicado pela imprensa (29/10/2018: 83.796,71 pontos) e contra a série 7 do Banco Central e o Yahoo `^BVSP` (diferença de 1 ponto, de arredondamento).

---

## 1. Introdução

### 1.1 Propósito
Guardar o **fechamento diário do Ibovespa oficial**, para que estudos de evento (relatórios de eleição, impeachment, circuit breakers) comparem cada papel com o índice de verdade, e não com um substituto.

O substituto que se usava é o ETF `BOVA11`, e ele erra o índice. Em 08/10/2018 o Ibovespa subiu **4,57%** (82.321,52 → 86.083,92) e o `BOVA11` subiu **5,27%** no mesmo dia: 0,7 ponto percentual de ruído, e mais em dias de estresse. Além disso o `BOVA11` só existe desde 02/12/2008, o que deixa as eleições de 2002 e 2006 sem referência.

### 1.2 Escopo
- Um fechamento por índice e por pregão, em pontos, com duas casas decimais.
- Série **desde 1968** (o que a B3 publica) até o último pregão.
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

Infrastructure/Persistence
├── Configurations/IndexQuoteConfiguration
└── Migrations/AddIndexQuotes

Planejado (não implementado):
Application/MarketData/Indexes   Import/{IIndexQuoteProvider, ...}, Commands, UseCases
Integrations/MarketData/B3/Indexes   Client Refit + provider
Api/GraphQL                      importIndexQuotes, indexQuotes
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
| RF-02 | Importar da B3 todos os pregões de um ano para um índice, de forma idempotente. | Não implementado |
| RF-03 | Expor a importação por mutation GraphQL. | Não implementado |
| RF-04 | Consultar os fechamentos por índice e intervalo de datas. | Não implementado |

## 4. Regras de negócio

- **RN-01 — Identidade.** Uma linha por **índice e pregão**, garantida por índice único em `(market_index, trading_date)`. A chave primária é um **`id` `bigint` comum**, como em `DailyQuote`: o usuário preferiu o `id` simples ao uso da chave natural composta como PK. O custo é um índice a mais, irrelevante aqui: o Ibovespa inteiro tem cerca de 15 mil linhas, contra 5,7 milhões de `daily_quotes`.
- **RN-02 — Fechamento positivo.** `close_value > 0`, no agregado (`index.quote.invalid`) e em `CHECK` no banco (`ck_index_quotes_close_value_positive`), para sobreviver a inserções que contornem o agregado.
- **RN-03 — Uma origem gravada.** A coluna `source` registra de onde veio o valor (hoje só `B3`). Quando houver mais de uma origem, o modelo já sabe distingui-las. Yahoo e Banco Central são usados apenas para conferir.
- **RN-04 — Idempotência.** Reimportar um ano não duplica: o que já existe e é igual é ignorado, o que mudou é atualizado, o que falta é criado.
- **RN-05 — Dia sem pregão não é linha.** A B3 devolve uma matriz dia × mês em que o dia sem pregão vem `null`; esses dias não geram registro.
- **RN-06 — Número em pt-BR.** O valor chega como texto (`"86.083,92"`); o ponto é separador de milhar e a vírgula é o decimal.
- **RN-07 — Ano parcial.** O ano corrente vem até o último pregão. Importar de novo mais tarde completa o ano.

## 5. Importação (especificada)

`GET https://sistemaswebb3-listados.b3.com.br/indexStatisticsProxy/IndexCall/GetPortfolioDay/{base64}`

O trecho `{base64}` é o JSON `{"language":"pt-br","index":"IBOV","year":"2018"}` codificado em base64. A resposta traz:

- `results[]`: uma entrada por dia do mês (`day`, de 1 a 31), com `rateValue1` a `rateValue12` (uma coluna por mês, texto pt-BR ou `null`);
- `min` e `max`: mínimo e máximo de cada mês, que **não** são fechamentos e devem ser ignorados.

Um ano por chamada. O mapeamento do enum `MarketIndex` para o código da B3 (`Ibovespa` → `IBOV`) fica no provider, na camada anticorrupção.

## 6. Superfície GraphQL (planejada)

- `importIndexQuotes(input: { index: IBOVESPA, year: 2018 })` devolve `fileFound`-equivalente, linhas lidas, criadas e atualizadas, como `importDailyQuotes`.
- `indexQuotes`: leitura Estilo B (`IQueryable` com `where`/`order`), já que é uma lista de dados públicos moldada pelo cliente.

Ambas seguem a regra do repositório de documentar o contrato em PT-BR no XML.

## 7. Riscos

- **Endpoint sem contrato.** É o proxy interno do site; a B3 pode mudar caminho, formato ou bloquear. Mitigação: um teste de contrato externo no workflow semanal `external-contracts.yml`, e não no CI de cada commit, para que uma queda da B3 não reprove um commit correto.
- **Verificação de certificado.** O `rb3` desliga a verificação de SSL nesse endpoint. Aqui não foi necessário: `curl` e Python funcionaram com a verificação padrão em 2026-10-11. Se isso mudar, é um sintoma a investigar, não um motivo para desligar a verificação.
- **Pontos inteiros no Banco Central.** Se um dia for preciso cruzar com a série 7, a comparação tem tolerância de 1 ponto.

## 8. Rastreabilidade

| Requisito / regra | Commit |
|---|---|
| RF-01, RN-02 (agregado, erro `index.quote.invalid`) | `106cec1` |
| RF-01, RN-01, RN-02 (tabela, índice único, CHECK) | `85c9361` |
| RF-02 a RF-04, RN-03 a RN-07 | pendente |

## 9. Cobertura de testes

- `IndexQuoteTests` (domínio): criação, fechamento não positivo, atualização.
- `DomainErrorCatalogTests`: inclui o código `index.quote.invalid`.
- `IndexQuotePersistenceTests` (Postgres real, Testcontainers): ida e volta com duas casas, índice único, pregões distintos coexistem, `CHECK` barra fechamento zero.

## 10. Próximos passos

1. Porta `IIndexQuoteProvider` e provider da B3 (parse do base64 e da matriz dia × mês), com linhas reais do ano de 2018 como fixture.
2. `ImportIndexQuotesCommandHandler` por upsert, com a validação cruzada: o número de pregões do índice em um ano deve bater com os dias distintos de `daily_quotes` (em 2018, 245 e 245).
3. Mutation `importIndexQuotes` e query `indexQuotes`.
4. Importar 1994 até hoje e refazer os relatórios de evento com o índice real.
