# SRS — Relatórios de combustível (paridade, posto mais barato, preço médio por bandeira, estado e município)

- **Status:** **entregue, quatro dos cinco relatórios.** Revisado em 2026-10-11: o texto original cobria só os dois primeiros e ficou desatualizado quando o comparador por bandeira e o ranking entraram (2026-09-05). Falta a evolução temporal.

## 1. Introdução

### 1.1 Propósito

Documenta os relatórios construídos sobre a base de dados importada pelo slice de ANP (`docs/srs/0002-fuel-anp.md`), cujo §10 já apontava "Relatórios" como o objetivo real do módulo. Cobre quatro dos cinco relatórios listados lá: **paridade etanol×gasolina**, **posto mais barato**, **preço médio por bandeira** e **ranking por estado e por município**.

### 1.2 Escopo

Dentro: leitura sobre `FuelPrice`/`FuelStation` já persistidos; nenhuma escrita nova. Os relatórios usam três estilos de leitura diferentes — decisão registrada porque cada um exigiu um caminho técnico distinto (ver §2.1).

Fora: **evolução temporal** do preço (precisa decidir a granularidade, por coleta ou por mês, antes de repetir o padrão) e `FilterInputType`/`SortInputType` dedicados. O `FuelStationType` curado, que o texto original listava como ausente, existe desde `bb25f16` (só descrições, vinculação implícita).

### 1.3 Definições

- **Style A / Style B** — ver `CLAUDE.md` § Persistence. Style A: resolver → UseCase → QueryHandler → DTO materializado. Style B: resolver devolve `IQueryable`, Hot Chocolate compõe filtro/ordenação/paginação/projeção por cima, tentando traduzir tudo pra uma única consulta SQL.
- **TDR** — Technical Debt Record, `docs/debt/`. Documenta um atalho técnico assumido deliberadamente (ver TDR-001, referenciado abaixo).
- **Greatest-n-per-group** — padrão de consulta pra "a linha mais recente/maior de cada grupo", sem usar `GroupBy` aninhado (que não traduz — ver RN-02).

## 2. Descrição geral

### 2.1 Posição na arquitetura

Os dois relatórios ficam em `Application/MarketData/Fuel/`, mas seguem caminhos diferentes:

- **`ethanolGasolineParity`** — Style B **variante em memória** (TDR-001). Segue o padrão UseCase→QueryHandler porque o cálculo (casar a coleta mais recente de etanol com a de gasolina do mesmo posto) não traduziu pra SQL contra Postgres real (`GroupBy` duplo com projeção de navegação — ver RN-02). O handler materializa as linhas cruas e monta o resultado em memória; o `[UseFiltering]/[UseSorting]` do resolver filtra sobre essa lista já em memória, não sobre o banco.
- **`cheapestFuelPrices`** — Style B **de verdade**, sem materialização. Introduz duas peças novas no projeto:
  - **`IFuelReads`/`FuelReads`** (`Application/MarketData/Fuel/Queries/`) — porta de leitura por feature, um passthrough fino sobre `IApplicationDbContext` que resolve "a coleta mais recente por posto" via subquery correlacionada com `MAX` (não `GroupBy`), mantendo o resultado como `IQueryable` translúcido. Padrão trazido de outro projeto do autor (IcrHub), adaptado porque mantém o resolver longe de `IApplicationDbContext` direto — o `CLAUDE.md` já proíbe o boundary de tocar porta sem passar por uma superfície da Application.
  - **`FuelPriceType`** (`Api/GraphQL/Types/`) — primeira vez que uma entidade de domínio (não um DTO) é exposta direto no schema GraphQL, curada por `ObjectType<FuelPrice>.BindFieldsExplicitly()`.

### 2.2 Fluxo

```
ethanolGasolineParity:
resolver → IFindEthanolGasolineParityUseCase → QueryHandler
  → materializa (Include + ToListAsync) → agrupa em memória → IQueryable in-memory
  → [UseFiltering]/[UseSorting] filtram/ordenam em memória

cheapestFuelPrices:
resolver → IFuelReads.LatestPricesByProduct(product)
  → subquery-MAX (IQueryable aberto, ainda não executado)
  → [UsePaging]/[UseProjection]/[UseFiltering]/[UseSorting] compõem e traduzem
  → uma única consulta SQL
```

## 3. Requisitos funcionais

### RF-01 — Comparar o preço de etanol e gasolina por posto

Dado o preço mais recente de etanol e de gasolina comum de um posto, o sistema deve calcular a razão entre eles e indicar se o etanol é vantajoso pela regra dos 70%. Postos sem coleta recente de um dos dois produtos ficam fora do resultado.

### RF-02 — Listar o preço mais recente de um produto, ordenável por preço

Dado um produto (`FuelProduct`), o sistema deve listar a coleta mais recente de cada posto pra aquele produto, permitindo ao cliente ordenar (ex.: por preço, pra achar o mais barato).

### RF-03 — Filtrar por atributos do posto via navegação

O cliente deve poder filtrar `cheapestFuelPrices` por campos do posto (ex.: `fuelStation.state`) sem que isso exija um resolver ou parâmetro dedicado — a navegação já filtrável faz parte do contrato Style B.

### RF-04 — Paginar os resultados

`cheapestFuelPrices` deve suportar paginação por cursor (`first`/`after`, padrão Relay), pra não obrigar o cliente a buscar a lista inteira.

### RF-05 — Preço médio por bandeira e estado

Dado um produto e, opcionalmente, um estado, o sistema deve devolver o preço médio de venda por par (bandeira, UF), sobre a última coleta de cada posto, com a quantidade de postos considerados. Resposta: `BrandAveragePriceResult { brand, state, averagePrice, stationCount }`.

### RF-06 — Ranking de preço médio por estado e por município

Dado um produto, o sistema deve devolver o preço médio por estado (`StateAveragePriceResult`) e por município (`MunicipalityAveragePriceResult`, com UF e filtro opcional por estado), ambos com a quantidade de postos. O cliente ordena por `averagePrice` para obter o ranking.

## 4. Regras de negócio

### RN-01 — Regra dos 70%

Etanol é vantajoso quando `preçoEtanol / preçoGasolina < 0.70`. Limiar fixo no domínio (`EthanolGasolineParity`, `AdvantageousThreshold = 0.7m`), sem configuração — mesma decisão de RNF-03 do simulador de financiamento (v1 sem `appsettings`).

### RN-02 — `GroupBy` duplo com navegação não traduz pra SQL

Verificado contra Postgres real via Testcontainers, não assumido: encadear um segundo `GroupBy` em cima de uma projeção já agrupada ("top-1 por grupo"), combinado com acesso a uma propriedade de navegação (`FuelStation`) dentro do mesmo `Select`, faz o EF lançar `ProjectionBindingExpression ... could not be translated` em tempo de execução. Duas soluções distintas foram aplicadas neste slice:

- **`ethanolGasolineParity`**: materializar as linhas filtradas primeiro (`Include` + `ToListAsync`), agrupar em memória com LINQ-to-Objects. Vira dívida técnica — TDR-001.
- **`cheapestFuelPrices`**: evitar `GroupBy` desde o início, usando subquery correlacionada com `MAX(CollectedOn)` por posto — traduz pra SQL sem materializar nada.

### RN-03 — Só a coleta mais recente conta

Nos dois relatórios, quando um posto tem mais de uma coleta de um produto (semanas diferentes), só a de `CollectedOn` mais recente entra no resultado — coletas antigas nunca aparecem, mesmo que o preço seja mais vantajoso.

### RN-05 — Agregado com `where`/`order` não traduz: materializa-se o resultado já agregado

Os três relatórios de média (RF-05, RF-06) usam `GroupBy` com `Average` e `Count`. O `GroupBy` em si traduz para SQL, mas o `[UseFiltering]`/`[UseSorting]` do Hot Chocolate reconstrói a projeção para chegar ao campo pedido, e o EF não consegue recompor a agregação por trás de um membro já projetado: lança `could not be translated` **em tempo de execução, só quando o cliente ordena ou filtra por `averagePrice` ou `stationCount`** (uma listagem simples parece funcionar). Foi o que aconteceu com `averagePricesByBrand` no dia em que entrou (`da5a3cf`).

A correção é materializar o agregado dentro de `IFuelReads` (`ToListAsync` + `.AsQueryable()`) antes de devolver. É seguro aqui porque o resultado depois do `GroupBy` por bandeira, estado ou município tem de dezenas a poucas centenas de linhas, ao contrário da dívida do TDR-001, que materializa a tabela crua. O custo pesado (varredura e agregação) continua sendo SQL; só o resultado pequeno vai para a memória. A alternativa de empurrar o agregado como coluna comum (`FromSqlInterpolated` contra uma entidade sem chave) foi considerada e **rejeitada por ora**: SQL escrito à mão por relatório, um conceito novo (entidade sem chave) e perda do reaproveitamento da subquery correlacionada, por um ganho que não aparece nesse volume.

Depurar isso exige `IncludeExceptionDetails` ligado (`Program.cs`, só em `Development`): sem ele o cliente vê só `Unexpected Execution Error` e o servidor não registra nada.

### RN-04 — Filtro do Hot Chocolate não é automaticamente curado pelo `ObjectType`

`BindFieldsExplicitly()` no `FuelPriceType` controla o que o cliente **vê** na saída — não controla, por si só, o que ele pode **filtrar**. O `[UseFiltering]` do Hot Chocolate infere filtros a partir do tipo `.NET` (a entidade `FuelPrice`), não do `ObjectType` curado. Se algum campo precisar ficar de fora do filtro sem estar já fora da saída, é necessário um `FilterInputType<FuelPrice>` próprio, com seu próprio `BindFieldsExplicitly()`, aplicado via `[UseFiltering<T>]` — não implementado neste slice (nenhum campo de `FuelPrice` precisou dessa restrição ainda).

## 5. Requisitos não-funcionais

- **RNF-01 (SQL de verdade em `cheapestFuelPrices`):** filtro, ordenação e paginação devem chegar como `WHERE`/`ORDER BY`/`LIMIT` no Postgres, não em memória. Verificado contra banco real, incluindo filtro por navegação (`fuelStation.state`).
- **RNF-02 (Dívida assumida em `ethanolGasolineParity`):** o filtro desse relatório roda em memória (RN-02) — aceito deliberadamente, registrado em TDR-001, com o custo por chamada documentado lá (~138 mil linhas lidas do banco a cada execução, em 2026-08). `[UsePaging]` (2026-09-06) corta o payload devolvido ao cliente, mas não muda esse custo: o `Skip`/`Take` roda sobre a lista já materializada em memória, depois da leitura completa do banco.
- **RNF-03 (Ordem dos atributos Hot Chocolate):** `[UsePaging]` → `[UseProjection]` → `[UseFiltering]` → `[UseSorting]`, nessa ordem, no resolver — ordem errada quebra o pipeline de middleware silenciosamente ou lança erro só em tempo de execução (`Projection provider not found` foi um erro real deste slice, por esquecer `.AddProjections()` no builder).

## 6. Interface — GraphQL

```graphql
type Query {
  ethanolGasolineParity: EthanolGasolineParityResultConnection!
  cheapestFuelPrices(product: FuelProduct!): FuelPriceConnection!
  averagePricesByBrand(product: FuelProduct!, state: String): BrandAveragePriceResultConnection!
  averagePriceByState(product: FuelProduct!): StateAveragePriceResultConnection!
  averagePriceByMunicipality(product: FuelProduct!, state: String): MunicipalityAveragePriceResultConnection!
}

type BrandAveragePriceResult { brand: String!  state: String!  averagePrice: Decimal!  stationCount: Int! }
type StateAveragePriceResult { state: String!  averagePrice: Decimal!  stationCount: Int! }
type MunicipalityAveragePriceResult { municipality: String!  state: String!  averagePrice: Decimal!  stationCount: Int! }

type EthanolGasolineParityResult {
  stationName: String!
  brand: String!
  municipality: String!
  state: String!
  ethanolPrice: Decimal!
  gasolinePrice: Decimal!
  ratio: Decimal!
  isEthanolAdvantageous: Boolean!
}

type FuelPrice {
  id: Int!
  fuelStation: FuelStation!
  product: FuelProduct!
  collectedOn: Date!
  salePrice: Decimal!
  purchasePrice: Decimal
  measureUnit: String!
}
```

`ethanolGasolineParity` aceita `where`/`order`/`first`/`after` (Style B em memória — RN-02/RNF-02; `[UsePaging]` adicionado depois do slice inicial, ver Follow-ups). `cheapestFuelPrices` aceita `where`/`order`/`first`/`after` (Style B com SQL pushdown), incluindo filtro em `fuelStation { state, municipality, ... }` por navegação.

## 7. Rastreabilidade (requisito → commit)

| Commit | Entrega |
|---|---|
| `7de6927` | expõe `FuelStations`/`FuelPrices` em `IApplicationDbContext` — pré-requisito de leitura |
| `44146b3` | `EthanolGasolineParity` (calculador de domínio) + testes — RN-01 |
| `45f4abd` | Query/Handler de paridade + teste contra Postgres real — RF-01, RN-02 (descoberta do limite do `GroupBy`) |
| `c353883` | UseCase de paridade + teste + wiring de DI |
| `d5f00de` | `ethanolGasolineParity` via Style B em memória (`HotChocolate.Data`) — RNF-02 |
| `3f2e060` | TDR-001 — registra a dívida da variante em memória |
| `f7a1f84` | `cheapestFuelPrices` via Style B de verdade (`IFuelReads` + `FuelPriceType`) — RF-02, RF-03, RN-02 (subquery-`MAX`), RNF-01 |
| `ed2eabf` | Paginação (`[UsePaging]`) em `cheapestFuelPrices` — RF-04, RNF-03 |
| `d889f4b` | `averagePricesByBrand` (`GroupBy` por bandeira e estado) — RF-05 |
| `da5a3cf` | Materializa o agregado antes de filtrar e ordenar — RN-05 |
| `cfcde1d` | `averagePriceByState` e `averagePriceByMunicipality` — RF-06 |
| `bb25f16` | Contrato GraphQL documentado em PT-BR e `FuelStationType` |

## 8. Cobertura de testes

| Nível | Cobre |
|---|---|
| Domínio (`EthanolGasolineParityTests`) | `Ratio` calcula a divisão correta; `IsEthanolAdvantageous` no limite exato de 70% (não vantajoso) e abaixo/acima. |
| Application (`FindEthanolGasolineParityUseCaseTests`) | UseCase delega ao handler e devolve o resultado, sem lógica própria. |
| Infraestrutura — Testcontainers (`FindEthanolGasolineParityQueryHandlerTests`) | Compara postos com os dois produtos; exclui posto com só um produto; usa a coleta mais recente quando há mais de uma — contra Postgres real, depois da correção de RN-02. |
| Infraestrutura — Testcontainers (`FuelReadsTests`) | `LatestPricesByProduct` devolve a coleta mais recente de cada posto, sem misturar postos nem produtos — valida a subquery-`MAX` contra SQL real. |
| Infraestrutura — Testcontainers (`FuelReadsAveragePriceByBrandTests`, `FuelReadsRankingTests`) | Média por bandeira e UF, por estado e por município sobre a última coleta de cada posto, com a contagem de postos e o filtro por estado — contra Postgres real. |
| Manual (GraphQL/Nitro) | `where`/`order`/`first` testados de ponta a ponta em `cheapestFuelPrices`, incluindo filtro em navegação (`fuelStation.state`) — sem teste automatizado ainda (ver §9). |

## 9. Follow-ups conhecidos

- **TDR-001 em aberto** — `ethanolGasolineParity` continua filtrando em memória; `[UsePaging]` (2026-09-06, motivado pelo consumo desse campo no app mobile) reduz o payload devolvido, não o custo de leitura no banco. Ver `docs/debt/0001-ethanol-gasoline-parity-in-memory-filter.md` pra critério de quando resolver.
- **Sem teste automatizado do `cheapestFuelPrices` fim a fim** — a validação de `where`/`order`/`first`/filtro em navegação foi manual (GraphQL/Nitro). Um `Api.FunctionalTests` cobrindo isso ficou pendente.
- **`FuelStationType` existe, mas só com descrições** (`bb25f16`): vinculação implícita, sem `BindFieldsExplicitly`. Os VOs `Cnpj`/`Cep` seguem sem exposição em GraphQL.
- **Evolução temporal**, o último relatório do §10 da SRS de ANP. Precisa decidir a granularidade (por coleta? por mês?) antes de repetir o padrão `IFuelReads` com agregado materializado (RN-05).
- **Sem teste funcional dos relatórios de média**, que são cobertos só no nível de `FuelReads` contra Postgres. A quebra de RN-05 só apareceu com um cliente real ordenando por campo agregado, e um teste funcional com `order: { averagePrice: ASC }` teria pegado.
- **`FilterInputType`/`SortInputType` dedicados** — não precisou ainda (RN-04), mas é o próximo passo se algum campo de `FuelPrice` precisar ficar de fora do filtro sem sair da saída.
