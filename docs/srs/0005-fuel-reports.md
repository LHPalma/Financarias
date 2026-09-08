# SRS — Relatórios de combustível (paridade etanol×gasolina e posto mais barato)

## 1. Introdução

### 1.1 Propósito

Documenta os dois primeiros relatórios construídos sobre a base de dados importada pelo slice de ANP (`docs/srs/0002-fuel-anp.md`), cujo §10 já apontava "Relatórios" como o objetivo real do módulo, ainda não construído. Esta SRS cobre a entrega de dois dos cinco relatórios listados lá: **paridade etanol×gasolina** e **posto mais barato**.

### 1.2 Escopo

Dentro: leitura sobre `FuelPrice`/`FuelStation` já persistidos; nenhuma escrita nova. Os dois relatórios usam estilos de leitura diferentes — decisão registrada porque cada um exigiu um caminho técnico distinto (ver §2.1).

Fora: os outros três relatórios do §10 da SRS de ANP (comparador por bandeira, ranking por município/estado, evolução temporal) — candidatos a Style B "de verdade", mesma técnica do posto mais barato, mas não implementados aqui. Também fora: `FuelStationType` curado (hoje a navegação `fuelStation` no schema é gerada por convenção, sem `BindFieldsExplicitly`) e `FilterInputType`/`SortInputType` dedicados.

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

## 4. Regras de negócio

### RN-01 — Regra dos 70%

Etanol é vantajoso quando `preçoEtanol / preçoGasolina < 0.70`. Limiar fixo no domínio (`EthanolGasolineParity`, `AdvantageousThreshold = 0.7m`), sem configuração — mesma decisão de RNF-03 do simulador de financiamento (v1 sem `appsettings`).

### RN-02 — `GroupBy` duplo com navegação não traduz pra SQL

Verificado contra Postgres real via Testcontainers, não assumido: encadear um segundo `GroupBy` em cima de uma projeção já agrupada ("top-1 por grupo"), combinado com acesso a uma propriedade de navegação (`FuelStation`) dentro do mesmo `Select`, faz o EF lançar `ProjectionBindingExpression ... could not be translated` em tempo de execução. Duas soluções distintas foram aplicadas neste slice:

- **`ethanolGasolineParity`**: materializar as linhas filtradas primeiro (`Include` + `ToListAsync`), agrupar em memória com LINQ-to-Objects. Vira dívida técnica — TDR-001.
- **`cheapestFuelPrices`**: evitar `GroupBy` desde o início, usando subquery correlacionada com `MAX(CollectedOn)` por posto — traduz pra SQL sem materializar nada.

### RN-03 — Só a coleta mais recente conta

Nos dois relatórios, quando um posto tem mais de uma coleta de um produto (semanas diferentes), só a de `CollectedOn` mais recente entra no resultado — coletas antigas nunca aparecem, mesmo que o preço seja mais vantajoso.

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
}

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

## 8. Cobertura de testes

| Nível | Cobre |
|---|---|
| Domínio (`EthanolGasolineParityTests`) | `Ratio` calcula a divisão correta; `IsEthanolAdvantageous` no limite exato de 70% (não vantajoso) e abaixo/acima. |
| Application (`FindEthanolGasolineParityUseCaseTests`) | UseCase delega ao handler e devolve o resultado, sem lógica própria. |
| Infraestrutura — Testcontainers (`FindEthanolGasolineParityQueryHandlerTests`) | Compara postos com os dois produtos; exclui posto com só um produto; usa a coleta mais recente quando há mais de uma — contra Postgres real, depois da correção de RN-02. |
| Infraestrutura — Testcontainers (`FuelReadsTests`) | `LatestPricesByProduct` devolve a coleta mais recente de cada posto, sem misturar postos nem produtos — valida a subquery-`MAX` contra SQL real. |
| Manual (GraphQL/Nitro) | `where`/`order`/`first` testados de ponta a ponta em `cheapestFuelPrices`, incluindo filtro em navegação (`fuelStation.state`) — sem teste automatizado ainda (ver §9). |

## 9. Follow-ups conhecidos

- **TDR-001 em aberto** — `ethanolGasolineParity` continua filtrando em memória; `[UsePaging]` (2026-09-06, motivado pelo consumo desse campo no app mobile) reduz o payload devolvido, não o custo de leitura no banco. Ver `docs/debt/0001-ethanol-gasoline-parity-in-memory-filter.md` pra critério de quando resolver.
- **Sem teste automatizado do `cheapestFuelPrices` fim a fim** — a validação de `where`/`order`/`first`/filtro em navegação foi manual (GraphQL/Nitro). Um `Api.FunctionalTests` cobrindo isso ficou pendente.
- **`FuelStationType` não existe** — a navegação `fuelStation` em `FuelPriceType` é exposta por convenção (Hot Chocolate reflete `FuelStation` inteiro), sem `BindFieldsExplicitly` própria. Os VOs `Cnpj`/`Cep` nunca foram expostos em GraphQL neste projeto; se algum campo deles aparecer de um jeito estranho no schema, é hora de criar o tipo curado.
- **Outros 3 relatórios do §10 da SRS de ANP** — comparador por bandeira, ranking por município/estado, evolução temporal. Mesma técnica de `cheapestFuelPrices` (subquery-`MAX` + `IFuelReads`), sem o problema de RN-02.
- **`FilterInputType`/`SortInputType` dedicados** — não precisou ainda (RN-04), mas é o próximo passo se algum campo de `FuelPrice` precisar ficar de fora do filtro sem sair da saída.
