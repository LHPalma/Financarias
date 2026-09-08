# SRS — Combustíveis / Import ANP (Fuel)

- **Data:** 2026-07-19
- **Feature:** `MarketData/Fuel` + `Integrations/MarketData/Anp/Fuel`
- **Status:** entregue e commitado na `main` (18 commits), 251 testes verdes, import validado ao vivo contra o arquivo real da ANP
- **Fonte externa:** [ANP — Série Histórica de Preços de Combustíveis](https://www.gov.br/anp/pt-br/centrais-de-conteudo/dados-abertos/serie-historica-de-precos-de-combustiveis) (arquivo `ca-AAAA-SS.zip`, sem chave)

---

## 1. Introdução

### 1.1 Propósito
Especificar os requisitos da **ingestão de preços de combustível** do Finançarias: baixar, parsear e persistir a série histórica posto-a-posto da ANP, formando a base de dados para o futuro **módulo de economização** (comparador de preço por bandeira, posto mais barato próximo, paridade etanol×gasolina).

É o **primeiro slice de import/ETL com escrita de verdade** e o que inaugura o **padrão Command** (`ICommand`/`ICommandHandler`), em contraste com os slices anteriores, que eram passthrough de leitura on-demand.

### 1.2 Escopo
- Download do arquivo semestral `ca-AAAA-SS.zip` da ANP (com fallback para o semestre anterior).
- Parse em streaming do CSV (~72 MB, ~422 mil linhas) e tradução para o modelo de domínio.
- Persistência com **upsert idempotente** de postos e preços.
- Disparo manual via mutation GraphQL `importFuelPrices`.

**Fora de escopo (hoje):** relatórios/consultas sobre os dados (o objetivo final do módulo), agendamento automático (cron/BackgroundService), multi-país (EIA/EUA), geocoding + PostGIS para "postos próximos", arquivo mensal agregado por município.

### 1.3 Definições
| Termo | Significado |
|---|---|
| **ANP** | Agência Nacional do Petróleo — publica a série histórica de preços. |
| **`ca-AAAA-SS`** | Arquivo semestral "Combustíveis Automotivos": posto-a-posto, **com bandeira**. `01` = jan–jun, `02` = jul–dez. |
| **Revenda** | O posto (identificado pelo CNPJ). |
| **Bandeira** | Rede à qual o posto pertence (Vibra, Ipiranga, Raízen…). `BRANCA` = sem bandeira. |
| **Coleta** | Observação de preço num posto, para um produto, numa data. |
| **Upsert** | Insere se não existe; atualiza se mudou; ignora se idêntico. |
| **Miss** | Linha malformada/incompleta — **pulada**, não é erro. |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura (Clean Architecture)
```
Domain
├── LegalEntities/Cnpj                     (VO genérico, 14 dígitos)
├── Geography/Region                       (enum, macrorregiões IBGE)
└── MarketData/Fuel
    ├── FuelProduct                        (enum curado, 6 produtos)
    ├── FuelStation                        (aggregate root — chave natural CNPJ)
    └── FuelPrice                          (aggregate root — nav FuelStation + StationId)

Application
├── Common/Messaging/{ICommand, ICommandHandler}   (padrão Command, novo)
├── Common/Persistence/IUnitOfWork                 (descarte de entidades rastreadas)
└── MarketData/Fuel
    ├── Import/{IFuelPriceProvider, FuelPriceImportItem, FuelImportResult}
    ├── Specifications/{StationsByCnpjs, PricesByStationsAndDates}
    ├── Commands/{ImportFuelPricesCommand, ImportFuelPricesCommandHandler}
    └── UseCases/{IImportFuelPricesUseCase + impl}

Infrastructure/Persistence
├── Converters/{CnpjConverter, CepConverter}       (VO ↔ string)
├── Configurations/{FuelStationConfiguration, FuelPriceConfiguration}
├── UnitOfWork                                     (ChangeTracker.Clear)
└── Migrations/{AddFuelStationsAndPrices, AddFuelPriceSalePriceCheck}

Integrations/MarketData/Anp/Fuel
├── Clients/IAnpFuelClient                         (Refit, devolve Stream)
└── Providers/AnpFuelProvider                      (implementa IFuelPriceProvider)

Api/GraphQL/Mutation.cs                            (importFuelPrices)
```

### 2.2 Fluxo (escrita — resolver → UseCase → CommandHandler)
`mutation importFuelPrices` → `IImportFuelPricesUseCase` (monta o Command) → `ImportFuelPricesCommandHandler` → consome `IFuelPriceProvider` em **streaming**, acumula lotes de 5.000 e faz upsert via `IRepository<FuelStation>` / `IRepository<FuelPrice>` + `Specification`.

---

## 3. Requisitos funcionais

### RF-01 — Baixar o arquivo semestral vigente da ANP
Obter `ca-AAAA-SS.zip` do semestre corrente. Se ainda não publicado (404 — normal no início de um semestre), **cair para o semestre anterior**.

### RF-02 — Transmitir as linhas parseadas em streaming
O provider expõe `IAsyncEnumerable<FuelPriceImportItem>`, entregando item a item, sem materializar as ~422 mil linhas.

### RF-03 — Traduzir o CSV para o modelo de domínio
Mapear as 16 colunas posicionais para tipos de domínio: `Cnpj`, `Region`, `FuelProduct`, `Cep`, `DateOnly`, `decimal`.

### RF-04 — Persistir postos com dedup por CNPJ
Cada CNPJ vira um `FuelStation` único; reimportação não duplica.

### RF-05 — Upsert de preços por chave natural
Chave `(posto, produto, data da coleta)`: insere novos, atualiza os que mudaram, ignora idênticos.

### RF-06 — Reportar o resultado do import
Devolver `rowsProcessed`, `stationsCreated`, `pricesCreated`, `pricesUpdated`.

### RF-07 — Disparo manual via GraphQL
Mutation `importFuelPrices`.

### RF-08 — Vocabulário curado de produtos e regiões
`FuelProduct` (6) e `Region` (5, IBGE) como enums; valores fora do vocabulário fazem a linha ser pulada.

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | **Dois agregados, não um.** `FuelStation` e `FuelPrice` são raízes separadas. Preço é **série temporal ilimitada** (6 produtos × coleta semanal × anos); prendê-lo como coleção-filha do posto violaria "agregado pequeno" e tornaria o import inviável (carregar o histórico inteiro para inserir uma linha). |
| **RN-02** | **Relação por objeto, identidade por id.** `FuelPrice.Create` recebe o `FuelStation` (constrói a partir de um agregado válido, sem id órfão) e a entidade guarda a nav `FuelStation` **e** o FK `StationId` — este exposto como facilitador de leitura/join. É um amolecimento **consciente** da fronteira entre agregados, em troca de código navegável. Não há coleção reversa em `FuelStation` (`.WithMany()` sem argumento) — é ela que geraria a lista gigante. |
| **RN-03** | **Chave natural do posto = CNPJ**; **do preço = (posto, produto, data da coleta)**. Garantidas por índice único no banco. |
| **RN-04** | **Cadastro do posto não é refrescado** a cada import (só cria o que falta). Dado cadastral é estável; refrescar 422 mil vezes seria churn puro. |
| **RN-05** | **Miss ≠ falha.** Linha malformada (CNPJ inválido, produto/região fora do vocabulário, data ilegível, preço ≤ 0, nome vazio) é **pulada** com log; falha de transporte **propaga**. 404 no semestre corrente é caso esperado, não erro. |
| **RN-06** | **Bandeira é string normalizada** (trim + upper), não enum: são ~47 valores e a ANP adiciona novos. É dimensão de comparação, não carrega regra; `BRANCA` (sem bandeira) é só mais um valor. |
| **RN-07** | **Preço de venda deve ser positivo** — invariante do agregado (`InvalidFuelPriceException`) **e** `CHECK (sale_price > 0)` no banco. |
| **RN-08** | **Read-before-write via repositório**, nunca pelo contrato de leitura. O handler carrega os agregados existentes por `IRepository<T>` + `Specification`, de modo que a mutação passe pelos métodos do agregado. `IApplicationDbContext` continua exclusivo dos query handlers. |
| **RN-09** | **Import é idempotente.** Reexecutar é seguro e é o mecanismo de recuperação: uma execução interrompida é completada rodando de novo. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Memória):** o zip (~8,5 MB) é bufferizado (o `ZipArchive` exige stream seekable), mas o CSV de 72 MB é **descomprimido sob demanda** e nunca reside inteiro em memória.
- **RNF-02 (Change tracker):** o `ChangeTracker` é limpo ao fim de cada lote via `IUnitOfWork.ClearTracking()`. Sem isso, o contexto do request acumulava centenas de milhares de entidades — memória subindo a ~280 MB e `SaveChanges` degradando progressivamente.
- **RNF-03 (Anti-corrupção):** o provider lê por **índice de coluna** e traduz para tipos de domínio; o vocabulário da ANP (`"GASOLINA ADITIVADA"`, `"NE"`) não vaza para dentro.
- **RNF-04 (Localização do dado):** CSV com `;`, UTF-8 **com BOM**, decimal com vírgula (cultura pt-BR) e data `dd/MM/yyyy`.
- **RNF-05 (Configuração):** `Integrations:Anp:BaseUrl` no appsettings da API **e** dos testes de integração (o `AddIntegrations` valida a config de todas as integrações no boot).
- **RNF-06 (Timeouts):** `HttpClient` do ANP em 5 min (download grande) e `ExecutionTimeout` do Hot Chocolate em 10 min — o default de 30 s abortava o import (`HC0045`).
- **RNF-07 (Testabilidade):** o provider é testável sem rede (client stubado + zip montado em memória); o handler é testável sem banco (portas substituídas) e **com** banco real (Testcontainers).

---

## 6. Interface — GraphQL

```graphql
type Mutation {
  importFuelPrices: FuelImportResult!
}

type FuelImportResult {
  rowsProcessed: Int!
  stationsCreated: Int!
  pricesCreated: Int!
  pricesUpdated: Int!
}
```

Conveniência: `GET /` redireciona para `/graphql`.

---

## 7. Integração externa — ANP

- **Endpoint:** `GET https://www.gov.br/anp/pt-br/centrais-de-conteudo/dados-abertos/arquivos/shpc/dsas/ca/ca-{AAAA-SS}.zip`
- **Auth:** nenhuma. **User-Agent** enviado (o portal é sensível a cliente anônimo).
- **Formato:** zip com um CSV; `;`; UTF-8 BOM; decimal vírgula.
- **Colunas (posicionais):** `Regiao - Sigla; Estado - Sigla; Municipio; Revenda; CNPJ da Revenda; Nome da Rua; Numero Rua; Complemento; Bairro; Cep; Produto; Data da Coleta; Valor de Venda; Valor de Compra; Unidade de Medida; Bandeira`
- **Cadência:** coleta semanal (seg–sex), publicação ~sextas. O arquivo do semestre **cresce** a cada semana — daí o upsert.
- **Notas do dado real:** CNPJ vem com máscara **e espaço à esquerda**; `Valor de Compra` quase sempre vazio; cobertura é **amostral** (~416 municípios), não todo posto do Brasil — o preço é retrato semanal datado, não tempo real.

---

## 8. Rastreabilidade (requisito → commit)

| Commit | Entrega |
|---|---|
| `8bc5570` | `ICommand`/`ICommandHandler` — fundação do padrão Command |
| `2843643` | VO `Cnpj` + testes — RF-03 |
| `f7de61b` | enum `Region` (IBGE) — RF-08 |
| `c7d4232` | `FuelProduct`, `FuelStation`, `FuelPrice` + exceções + testes — RN-01, RN-02, RN-07 |
| `99ce8ca` | porta `IFuelPriceProvider` + DTOs de import — RF-02, RF-06 |
| `afa8ac9` | specifications de escrita — RN-08 |
| `f812fd1` | `ImportFuelPricesCommand` + handler de upsert + testes — RF-04, RF-05, RN-03, RN-04, RN-09 |
| `e5bdc28` | use case de import + teste — RF-07 |
| `e756b85` | value converters `Cnpj`/`Cep` |
| `c229fd6` | mapeamento EF + migration + testes de persistência — RN-03 |
| `5ed005e` | teste de upsert end-to-end contra Postgres real — RN-09 |
| `fe5261b` | client ANP + provider de streaming CSV + testes — RF-01, RF-02, RF-03, RN-05, RN-06 |
| `7574f60` | wiring DI (provider + import) e configuração — RNF-05 |
| `3434eff` | mutation `importFuelPrices` + `ExecutionTimeout` — RF-07, RNF-06 |
| `1627647` | redirect `/` → `/graphql` |
| `2f87c28` | porta `IUnitOfWork` + impl — RNF-02 |
| `4e70a2c` | limpeza do change tracker por lote — RNF-02 |
| `55452ab` | `CHECK (sale_price > 0)` + migration — RN-07 |

---

## 9. Cobertura de testes (251 no total, 0 falhas)

| Nível | Cobre |
|---|---|
| Domínio (`CnpjTests`) | normalização de máscara/espaço, `TryCreate`, igualdade por valor, `Formatted`. |
| Domínio (`FuelStationTests`, `FuelPriceTests`) | `Create` monta o agregado; nome vazio e preço ≤ 0 lançam a exceção específica; `UpdateDetails`/`UpdateValues` sobrescrevem e revalidam; a nav aponta para o posto. |
| Handler (unit) | cria posto+preços novos; não recria posto existente; atualiza preço alterado; **ignora preço idêntico** (nem insert nem save). |
| Use case | delega ao command handler e devolve o resultado. |
| Provider ANP (unit) | mapeia produto/região/decimal-vírgula/data/CEP; **6 casos de linha malformada** pulada; fallback de semestre no 404. |
| Persistência (Testcontainers) | round-trip posto+preço com `Include` da nav e VOs convertidos; os 2 índices únicos barrando duplicata; `Contains` sobre o VO `Cnpj` traduzindo para SQL. |
| Import e2e (Testcontainers) | reimportação insere data nova, atualiza preço mudado e pula idêntico, sem recriar posto — em `DbContext`s separados, como requests reais. |

---

## 10. Follow-ups conhecidos

**Performance da escrita (prioritário).** Medido rodando de verdade: o EF emite **um `INSERT ... RETURNING id` por linha** (~1000 statements por comando) — ~422 mil INSERTs individuais, na casa de minutos. A limpeza do change tracker (RNF-02) resolveu memória e degradação, mas não o volume de statements.
> **Próxima fatia decidida:** porta `IFuelPriceBulkWriter` (Application) + adapter Postgres (Infrastructure) com `COPY` binário para tabela de staging e dois comandos set-based — `INSERT INTO fuel_stations … ON CONFLICT (cnpj) DO NOTHING` e `INSERT INTO fuel_prices … SELECT s.id … ON CONFLICT (station_id, product, collected_on) DO UPDATE`. Bulk de **postos e preços**. Isso remove `EnsureStationsAsync`/`LoadExistingPricesAsync`/`TryApplyChanges` do handler e **torna o `ClearTracking()` desnecessário**. Os `IRepository<>` permanecem para escrita transacional de domínio — as duas portas coexistem. Custo consciente: as factories de domínio ficam fora do caminho bulk (mitigado pelo RN-07 no banco e pela validação no provider).

**Outros:**
- **Relatórios** — o objetivo do módulo ainda não foi construído: comparador por bandeira, posto mais barato, paridade etanol×gasolina (<70%), ranking por município/estado, evolução temporal. Exigem expor `FuelStations`/`FuelPrices` no `IApplicationDbContext` (leitura).
- **Agendamento** — hoje o disparo é manual e **síncrono dentro do request GraphQL** (segura a conexão por minutos). Deveria ser `BackgroundService`/cron com execução assíncrona.
- **Multi-país (EUA/EIA)** — a EIA só publica agregado (sem posto nem bandeira). Modelar como **read model unificado** (`country`, `geo_level`, `brand` nullable), não forçando na mesma tabela de ingestão.
- **Geocoding + PostGIS** — para "postos próximos": geocodificar por CNPJ no import, `geography(Point,4326)` + `ST_DWithin`/GiST.
  > **Desenho decidido para a resolução de localização (ainda não implementado).** Entrada do cliente pode chegar como `lat/lon` já resolvido, `Cep` ou endereço livre de pesquisa — sem flag explícita: o próprio tipo da entrada já diz se precisa resolver.
  > - **VO de união discriminada** `LocationQuery` (`Domain/Geography`, não específico de Fuel): `CoordinatesQuery(double Lat, double Lon)` | `CepQuery(Cep)` | `AddressQuery(string RawAddress)`.
  > - **Duas portas em Application**, uma por provider (nunca uma porta genérica forçando as duas assinaturas): `IAwesomeApiGeocodingProvider.ResolveAsync(Cep) -> Coordinates?` e `INominatimGeocodingProvider.ResolveAsync(string) -> Coordinates?`.
  > - **Um `ResolveLocationQueryHandler`** só (uma operação lógica, não três) — switch na variante: `CoordinatesQuery` é passthrough sem chamada externa; `CepQuery` chama a AwesomeAPI; `AddressQuery` chama a Nominatim.
  > - **Escolha de provider por tipo de entrada, validada empiricamente (2026-07-19):** testado o mesmo endereço (Av. Paulista 1578 / CEP 01310-200) nas duas APIs. Resultado CEP→coordenada: **AwesomeAPI 0,26 km** de erro vs. **Nominatim 42,28 km** (resolveu para outro município). Com rua+número completos a Nominatim empata em precisão rooftop (0 km) — mas a AwesomeAPI não aceita endereço livre, só CEP. Daí a divisão: **AwesomeAPI para `CepQuery`** (mais precisa nesse caso e sem exigir montar endereço), **Nominatim para `AddressQuery`** (único candidato sem chave para texto livre; exige filtrar por `place_rank`/`addresstype` — resultados tipo `postcode`/`suburb` devem ser tratados como miss, não aceitos como coordenada).
  > - **Nominatim tem rate limit de uso público** (1 req/s, scripts de longa duração restritos a 4 req/min, proíbe geocoding em massa) — inviabiliza usá-la para backfill de todos os postos; serve para resolução pontual (`AddressQuery` sob demanda), não para o backfill de `FuelStation`, que deve seguir só por CEP via AwesomeAPI.
- **Encoding** — assume UTF-8 (o arquivo real tem BOM). Se a ANP publicar Latin-1 em algum semestre, o parse de acentos quebra silenciosamente.
- **`ImportHolidaysUseCase`** — segue sem o padrão Command e lendo via `IApplicationDbContext`; é a exceção pendente de refatorar para o padrão estabelecido aqui (RN-08).
