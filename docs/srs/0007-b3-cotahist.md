# SRS — Cotações históricas da B3 / Import COTAHIST (Stocks)

- **Data:** 2026-10-10
- **Feature:** `MarketData/Stocks` (histórico) + `Integrations/MarketData/B3/Cotahist`
- **Status:** **especificado, não implementado.** O único item entregue é o ajuste do regex do `Ticker` (RN-09, commit `613863a`). Todo o resto é desenho a validar na implementação.
- **Fonte externa:** [B3 — Cotações históricas](https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/cotacoes-historicas/) (arquivo `COTAHIST_*.ZIP`, sem chave) e o [layout oficial](https://www.b3.com.br/data/files/33/67/B9/50/D84057102C784E47AC094EA8/SeriesHistoricas_Layout.pdf) (revisão de 2020).
- **Base empírica:** o layout abaixo foi conferido em **2026-10-10** contra o arquivo real `COTAHIST_D09102026.ZIP` (19.213 registros `01`, 1.847 deles de mercado à vista). Onde o arquivo diverge do PDF, vale o arquivo (ver §7.3).

---

## 1. Introdução

### 1.1 Propósito
Especificar a **ingestão da série histórica de cotações da B3**: baixar o `COTAHIST`, interpretar o layout de largura fixa e persistir o resumo diário de cada papel (abertura, máxima, mínima, média, fechamento, negócios, quantidade e volume).

É a primeira fonte de **histórico de preço de ativo** do Finançarias. A cotação de ação já existente (Brapi, `stockQuote`) é só o preço de agora, sem série.

### 1.2 Escopo
- Download de arquivo **anual**, **mensal** ou **diário** (o chamador escolhe o período).
- Parse em streaming do arquivo de largura fixa (245 caracteres por linha).
- Persistência com **upsert idempotente** de uma linha por (papel, pregão, tipo de mercado).
- Disparo manual via mutation GraphQL `importDailyQuotes` e leitura via query `dailyQuotes`.
- **Só mercado à vista:** `TPMERC` 010 (vista) e 020 (fracionário).

**Fora de escopo (hoje):**
- Opções (070/080), termo (030), exercício de opções (012/013) e leilão (017) — decisão do usuário: "à vista agora, resto depois". Ponto de extensão em RN-04.
- Ajuste por proventos e desdobramentos (a B3 publica preço **sem ajuste**, ver RN-08).
- Entidade de "instrumento"/"emissor" (o ISIN é guardado para ligar depois, ver RN-03).
- Agendamento automático, preço de oferta (`PREOFC`/`PREOFV`), preços de exercício e dados de termo.
- Guardar o TXT original (camada bronze). O projeto não tem essa camada; o parse vai direto para a tabela.

### 1.3 Definições
| Termo | Significado |
|---|---|
| **COTAHIST** | Arquivo de "cotações históricas" da B3: resumo de cada pregão por papel-mercado. |
| **Papel-mercado** | Um código de negociação em um tipo de mercado (a mesma raiz pode aparecer em vista e em opções). |
| **CODNEG** | Código de negociação (o ticker: `PETR4`, `B3SA3`, `AAPL34`…). |
| **TPMERC** | Tipo de mercado: `010` vista, `020` fracionário, `030` termo, `070`/`080` opções. |
| **CODBDI** | Código do boletim diário que classifica o papel (`02` lote padrão, `96` fracionário, `12` FII…). |
| **FATCOT** | Fator de cotação: o preço publicado vale para 1 ação (`1`), para um lote de 100 ou de 1.000. |
| **Miss** | Linha malformada ou fora de escopo: **pulada**, não é erro. |
| **Upsert** | Insere se não existe; atualiza se mudou; ignora se idêntico. |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura (Clean Architecture)
```
Domain
└── MarketData
    ├── Ticker                          (VO existente; regex ajustado, RN-09)
    ├── QuotePeriod                     (união discriminada: Year | Month | Day)
    └── Stocks
        ├── DailyQuote                  (aggregate root — chave natural ticker + pregão + mercado)
        └── QuoteMarketType             (enum: Cash, Fractional)

Application/MarketData/Stocks
├── Import/{IDailyQuoteProvider, DailyQuoteImportItem, DailyQuoteImportResult}
├── Specifications/QuotesByTickersAndDatesSpecification
├── Commands/{ImportDailyQuotesCommand, ImportDailyQuotesCommandHandler}
├── UseCases/{IImportDailyQuotesUseCase + impl}
└── Queries/{IStockReads, StockReads}   (leitura Style B; só quando a query chegar)

Infrastructure/Persistence
├── Configurations/DailyQuoteConfiguration
└── Migrations/AddDailyQuotes

Integrations/MarketData/B3/Cotahist
├── Clients/IB3CotahistClient          (Refit, devolve Stream)
├── CotahistLineParser                 (puro: string de 245 chars → item ou miss)
└── Providers/B3CotahistProvider       (implementa IDailyQuoteProvider)

Api/GraphQL
├── Mutations/StockMutations           (importDailyQuotes)
├── Queries/StockQueries               (dailyQuotes — junto da stockQuote existente)
└── Types/DailyQuoteType
```

### 2.2 Fluxo (escrita — resolver → UseCase → CommandHandler)
`mutation importDailyQuotes` → `IImportDailyQuotesUseCase` (valida o período e monta o Command) → `ImportDailyQuotesCommandHandler` → consome `IDailyQuoteProvider` em **streaming**, acumula lotes de 5.000 e faz upsert via `IRepository<DailyQuote>` + `Specification`, limpando o change tracker por lote (`IUnitOfWork.ClearTracking()`). É o mesmo molde do import da ANP (`docs/srs/0002-fuel-anp.md`).

---

## 3. Requisitos funcionais

### RF-01 — Escolher o período do import
O chamador informa um **dia**, um **mês** ou um **ano**. Cada forma mapeia para um arquivo: `COTAHIST_D{ddMMyyyy}.ZIP`, `COTAHIST_M{MMyyyy}.ZIP` ou `COTAHIST_A{yyyy}.ZIP`.

### RF-02 — Baixar o arquivo do período
Obter o ZIP da B3. Se o arquivo **não existe (404)**, o import termina sem erro e sem linhas (RN-06).

### RF-03 — Transmitir as linhas parseadas em streaming
O provider expõe `IAsyncEnumerable<DailyQuoteImportItem>`, item a item, sem materializar o arquivo.

### RF-04 — Traduzir o registro `01` para o modelo de domínio
Mapear os campos posicionais para `Ticker`, `DateOnly`, `QuoteMarketType`, `decimal`, `int` e `long`.

### RF-05 — Persistir com upsert por chave natural
Chave `(ticker, data do pregão, tipo de mercado)`: insere novas, atualiza as que mudaram, ignora idênticas.

### RF-06 — Reportar o resultado do import
Devolver o que foi lido, criado, atualizado e pulado, e se o arquivo existia.

### RF-07 — Disparo manual via GraphQL
Mutation `importDailyQuotes`.

### RF-08 — Consultar o histórico
Query `dailyQuotes` com filtro, ordenação e paginação (por ticker e por intervalo de datas, no mínimo).

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | **Um agregado, uma linha por papel-mercado-pregão.** `DailyQuote` é raiz de agregado sem filhos. O ticker é uma `string`/VO, não há entidade de papel: só o dado que o arquivo traz. |
| **RN-02** | **Chave natural `(ticker, data do pregão, tipo de mercado)`**, garantida por índice único. Verificado em 2026-10-10: nenhuma duplicata nas 1.847 linhas à vista; incluir o `CODBDI` na chave também não gerou duplicata. Fica de fora porque o BDI classifica, não identifica. |
| **RN-03** | **O ISIN (`CODISI`) é guardado**, mas não há FK nem entidade de instrumento. Todas as linhas à vista da amostra trazem ISIN começando com `BR`. É o gancho para ligar ao emissor quando esse modelo existir. |
| **RN-04** | **Filtro por `TPMERC`, não por `CODBDI`.** Só 010 e 020 entram. Filtrar por BDI quebraria: a amostra tem 11 BDIs diferentes no mercado à vista, e o PDF não documenta 4 deles (13, 34, 35 e 36). Opções, termo e demais mercados ficam como extensão: acrescentar valores a `QuoteMarketType` e liberar o filtro. |
| **RN-05** | **`CODBDI` é guardado como texto de 2 caracteres**, sem enum. A tabela do PDF está defasada em relação ao arquivo (RN-10). |
| **RN-06** | **Miss ≠ falha.** Linha com tamanho diferente de 245, tipo de registro desconhecido, ticker que não casa com `Ticker`, data ilegível, número ilegível, moeda diferente de `R$` ou violação da RN-07 é **pulada**, contada em `rowsSkipped` e logada. Falha de transporte (rede, 5xx, ZIP corrompido) **propaga**. **404 é caso esperado:** fim de semana, feriado e data futura não têm arquivo diário (verificado: `D10102026`, `D12102026`, `D07092026` e `D12102025` respondem 404). |
| **RN-07** | **Invariantes do agregado:** abertura, máxima, mínima, média e fechamento **> 0**; máxima ≥ mínima; negócios > 0; quantidade > 0. Valem no `Create`/`UpdateValues` **e** como `CHECK` no banco, para sobreviverem a caminhos que contornem o agregado. Nenhuma linha à vista da amostra viola isso. **Não há invariante sobre a média:** em 1 linha de 1.847 o `PREMED` caiu fora do intervalo [mínima, máxima]. |
| **RN-08** | **Preço é gravado como publicado, com o fator de cotação ao lado.** O `FATCOT` diz se o preço vale por ação (`1`) ou por lote (`100`, `1000`). Na amostra: 1.844 linhas com `1`, 2 com `1000` e 1 com `100`. Guardar o dado cru preserva a fidelidade à fonte; quem consome divide pelo fator. Os preços **não têm ajuste** por desdobramento, grupamento ou proventos (a própria B3 avisa), então uma série longa tem saltos nessas datas. É limitação conhecida, não bug. |
| **RN-09** | **O VO `Ticker` aceita dígito na raiz** (`^[A-Z][A-Z0-9]{3}\d{1,2}F?$`). O regex antigo, de 4 letras, rejeitava 15% do mercado à vista (279 de 1.847): `B3SA3`, BDRs (`V1IP34`, `BPIC39`) e ETFs (`B1003`). Com o novo, só `MRSA3BF` fica de fora (fracionário com sufixo de classe) e vira miss. A mudança vale também para `stockQuote` (Brapi). **Entregue** em `613863a`. |
| **RN-10** | **O arquivo é a fonte de verdade, não o PDF.** Divergências verificadas estão em §7.3. O parser não depende da tabela de BDI nem do conjunto de fatores de cotação do PDF. |
| **RN-11** | **Import é idempotente.** Reexecutar é seguro e é o mecanismo de recuperação. Reimportar o ano corrente (que a B3 reconstrói todo dia) atualiza só o que mudou. |
| **RN-12** | **Read-before-write via repositório**, nunca pelo contrato de leitura (mesma regra do import da ANP). O handler carrega os agregados existentes por `IRepository<DailyQuote>` + `Specification` e muda pelos métodos do agregado. |
| **RN-13** | **O trailer é conferência, não bloqueio.** Divergência entre o total do trailer e o número de registros `01` lidos gera log de aviso, e o import segue. Ver §7.3 sobre o que o trailer conta de fato. |
| **RN-14** | **Período inválido é erro de domínio.** Combinação que não é dia, mês ou ano (mês sem ano, mês fora de 1–12, ano antes de 1986 ou depois do atual, data futura) lança `stock.quoteperiod.invalid`. A validação fica no use case, antes de qualquer download. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Memória):** o ZIP anual tem ~91 MB e expande para bem mais de 1 GB (≈ 4,8 milhões de linhas, só ~10% à vista). O download vai para um **arquivo temporário** (`FileOptions.DeleteOnClose`), porque `ZipArchive` exige stream seekable e bufferizar 91 MB em `MemoryStream` não escala. A entrada é lida em **streaming**, linha a linha, sem materializar o TXT.
- **RNF-02 (Change tracker):** `IUnitOfWork.ClearTracking()` ao fim de cada lote de 5.000 (mesma razão do import da ANP).
- **RNF-03 (Anti-corrupção):** o parser lê por **posição** e traduz para tipos de domínio; o vocabulário da B3 (`TPMERC`, `CODBDI`) não vaza para além do provider e do enum curado.
- **RNF-04 (Formato do dado):** largura fixa, 245 caracteres por linha, terminador CRLF, **sem separador**. Preços são inteiros com 2 casas implícitas (`/ 100`), volume idem. Datas em `AAAAMMDD`. Codificação ANSI do Windows: ler como **Latin1**; as diferenças para a Windows-1252 estão só em 0x80–0x9F e não tocam ticker, número ou data.
- **RNF-05 (Configuração):** `Integrations:B3:BaseUrl` (padrão `https://bvmf.bmfbovespa.com.br`) no appsettings da API e dos testes, como a ANP. O `AddIntegrations` valida a configuração de todas as integrações no boot.
- **RNF-06 (Timeouts):** `HttpClient` da B3 com timeout alto (o anual tem 91 MB) e `ExecutionTimeout` do Hot Chocolate acima do tempo do import anual (ver risco em §10).
- **RNF-07 (Testabilidade):** o `CotahistLineParser` é função pura, testada com **linhas literais copiadas do arquivo real**; o provider, com client substituído e ZIP montado em memória; o handler, sem banco (portas substituídas) e **com** banco real (Testcontainers).
- **RNF-08 (Tamanho esperado):** ≈ 1,9 mil linhas à vista por pregão, ≈ 480 mil por ano (estimativa: 250 pregões × a amostra). Os arquivos de 1986 em diante estão disponíveis (`A1986` responde 200, 8,7 MB).

---

## 6. Interface — GraphQL (proposta)

```graphql
type Mutation {
  "Importa cotações do COTAHIST da B3. Público e pesado: qualquer um pode chamar hoje."
  importDailyQuotes(input: ImportDailyQuotesInput!): DailyQuoteImportResult!
}

input ImportDailyQuotesInput {
  "Pregão específico. Não combina com year/month."
  date: Date
  "Ano inteiro, ou o ano do mês quando month é informado."
  year: Int
  "Mês (1–12). Exige year."
  month: Int
}

type DailyQuoteImportResult {
  fileFound: Boolean!
  rowsRead: Int!
  rowsSkipped: Int!
  quotesCreated: Int!
  quotesUpdated: Int!
}

type Query {
  dailyQuotes(...): DailyQuoteConnection   # Style B: where / order / paginação
}
```

Combinações aceitas no input: só `date` (dia); só `year` (ano); `year` + `month` (mês). Qualquer outra lança `stock.quoteperiod.invalid` (RN-14). Escolhi três campos opcionais, e não um `@oneOf`, para manter o contrato simples; é uma troca que pode ser revista.

`DailyQuote` fica exposto direto, por um `ObjectType<DailyQuote>` com `BindFieldsExplicitly()`, como `FuelPriceType`. **O VO `Ticker` precisa ser declarado a partir do membro** (`descriptor.Field(q => q.Ticker).Name("ticker").Type<NonNullType<StringType>>().Resolve(...)`): declarar com `Field("ticker").Resolve(...)` compila, passa nos testes unitários e falha em runtime com `Unexpected Execution Error`, porque a projeção do EF não vê a coluna (CLAUDE.md, *Persistence*). Só um funcional contra banco real pega isso.

Toda a superfície nasce documentada em XML em PT-BR (regra da documentação do contrato GraphQL), incluindo que `importDailyQuotes` é público e pesado.

**Erros (`extensions.code`):** `stock.quoteperiod.invalid` (período), `stock.quote.invalid` (invariante do agregado, só alcançável por caminho que contorne o parser).

---

## 7. Integração externa — B3

### 7.1 Endpoint
- **URL:** `GET https://bvmf.bmfbovespa.com.br/InstDados/SerHist/{arquivo}`, onde `arquivo` é `COTAHIST_A{yyyy}.ZIP`, `COTAHIST_M{MMyyyy}.ZIP` ou `COTAHIST_D{ddMMyyyy}.ZIP`. O domínio `www.b3.com.br/pb/inst-dados/...` responde 404; este é o host que serve o arquivo.
- **Auth:** nenhuma.
- **Disponibilidade medida em 2026-10-10:** anual de 1986 (8,7 MB) a 2026 (91 MB, reconstruído diariamente); mensal de 2020, 2024 e 2026 (3 a 10 MB); diário de 02/09/2025 (332 KB) a 09/10/2026 (564 KB), sem que se tenha testado o limite de retenção. Dias sem pregão e datas futuras: 404.
- **Conteúdo do ZIP:** um TXT (no diário: `COTAHIST_D09102026.TXT`). O provider escolhe a entrada pela extensão `.TXT`, sem fixar o nome.
- **Cadência:** o diário do dia aparece de madrugada do dia seguinte ao pregão.

### 7.2 Layout do registro `01` (245 caracteres, posições a partir de 1)
| Campo | Posição | Tipo | Notas |
|---|---|---|---|
| `TIPREG` | 1–2 | `N(2)` | `00` header, `01` cotação, `99` trailer |
| `DATA` | 3–10 | `AAAAMMDD` | data do pregão |
| `CODBDI` | 11–12 | `X(2)` | RN-05 |
| `CODNEG` | 13–24 | `X(12)` | ticker, com espaços à direita |
| `TPMERC` | 25–27 | `N(3)` | RN-04 |
| `NOMRES` | 28–39 | `X(12)` | nome resumido |
| `ESPECI` | 40–49 | `X(10)` | espécie e classe (`ON NM`, `PN N1`, `DRN`…) |
| `PRAZOT` | 50–52 | `X(3)` | prazo do termo; não usado |
| `MODREF` | 53–56 | `X(4)` | moeda; `R$` em todas as linhas à vista da amostra |
| `PREABE` | 57–69 | `(11)V99` | abertura |
| `PREMAX` | 70–82 | `(11)V99` | máxima |
| `PREMIN` | 83–95 | `(11)V99` | mínima |
| `PREMED` | 96–108 | `(11)V99` | média |
| `PREULT` | 109–121 | `(11)V99` | último negócio (usado como **fechamento**) |
| `PREOFC` / `PREOFV` | 122–134 / 135–147 | `(11)V99` | ofertas; fora de escopo |
| `TOTNEG` | 148–152 | `N(5)` | número de negócios |
| `QUATOT` | 153–170 | `N(18)` | quantidade negociada |
| `VOLTOT` | 171–188 | `(16)V99` | volume financeiro |
| `PREEXE` | 189–201 | `(11)V99` | preço de exercício; fora de escopo |
| `INDOPC` | 202 | `N(1)` | fora de escopo |
| `DATVEN` | 203–210 | `AAAAMMDD` | vencimento; fora de escopo |
| `FATCOT` | 211–217 | `N(7)` | RN-08 |
| `PTOEXE` | 218–230 | `(07)V06` | fora de escopo |
| `CODISI` | 231–242 | `X(12)` | ISIN, RN-03 |
| `DISMES` | 243–245 | `9(3)` | número de distribuição; fora de escopo |

`00` (header): `COTAHIST.AAAA` nas posições 3–15, `BOVESPA` em 16–23 e a data de geração em 24–31. `99` (trailer): total de registros em 32–42.

**Tipos persistidos:** preços `numeric(13,2)`, volume `numeric(18,2)`, quantidade `bigint`, negócios `integer`. Os máximos vistos na amostra (92.549 negócios; 92,7 milhões de quantidade; R$ 2,79 bilhões de volume em um papel) cabem com folga.

### 7.3 Onde o arquivo diverge do PDF (verificado em 2026-10-10)
1. **`CODBDI` 13, 34, 35 e 36** respondem por 586 das 1.847 linhas à vista e **não estão na tabela anexa** do PDF. Os BDIs 34, 35 e 36 (583 linhas) são BDRs, a julgar pela `ESPECI` (`DRN`, `DRE`, `DR1`…).
2. **`FATCOT` 100** existe no arquivo; o PDF só documenta `1` e `1000`.
3. **O trailer conta só os registros `01`.** O PDF diz que inclui header e trailer, mas a amostra tem 19.213 registros `01`, header e trailer (19.215 linhas) e o campo traz 19.213.
4. **`PREMED` pode ficar fora de [mínima, máxima]** (1 linha de 1.847).
5. O nome do arquivo no header (`COTAHIST.2026`) vem assim mesmo no arquivo diário.

Um sexto ponto é de contexto, não de divergência: a revisão do PDF é de 2020, então o conferido hoje vale como o contrato real. O teste de contrato externo (§9) existe justamente para avisar quando o formato mudar.

---

## 8. Rastreabilidade (requisito → commit)

| Commit | Entrega |
|---|---|
| `613863a` | `Ticker` aceita dígito na raiz — RN-09 |

Os demais commits entram aqui conforme saírem. Ordem prevista: `QuotePeriod` e `DailyQuote` (domínio) → `CotahistLineParser` → porta, comando, handler e use case → client e provider → configuration e migration → GraphQL.

---

## 9. Cobertura de testes (planejada)

| Nível | Cobre |
|---|---|
| Domínio (`TickerTests`) | **Entregue:** raiz com dígito (`B3SA3`, `AAPL34`, `V1IP34`, `B1003`) e entradas inválidas novas. |
| Domínio (`QuotePeriodTests`, `DailyQuoteTests`) | período válido e inválido por forma; `Create` e `UpdateValues` revalidam RN-07; código do erro como literal. |
| Parser (unit) | linhas literais copiadas do arquivo real: ação comum, BDR, ETF, fracionário, `FATCOT` 100 e 1000; preço `/100`; **miss** para tamanho errado, ticker fora do padrão (`MRSA3BF`), mercado fora de escopo, moeda ≠ `R$`, número ilegível. |
| Provider (unit) | ZIP montado em memória; header e trailer ignorados; aviso quando o trailer diverge; 404 vira "arquivo não encontrado"; falha de transporte propaga. |
| Handler (unit) | cria; atualiza o alterado; ignora o idêntico; limpa o tracker por lote. |
| Use case | período inválido não chega ao handler; `date` / `year` / `year+month` viram a forma certa. |
| Persistência (Testcontainers) | round-trip com o VO `Ticker` convertido; índice único barrando duplicata; `CHECK` barrando preço ≤ 0; upsert e2e em `DbContext`s separados. |
| Funcional (GraphQL) | `dailyQuotes` com `where`/`order`/paginação **contra banco real** (pega o problema do VO na projeção); `importDailyQuotes` com período inválido. |
| Contrato externo (`external-contracts.yml`) | baixa um diário real recente e confere: 245 caracteres por linha, header e trailer presentes, parse sem miss inesperado. Fora do CI normal, pelo mesmo motivo do ViaCEP e da Anbima. |

---

## 10. Riscos e decisões em aberto

- **Performance da escrita no ano inteiro (principal risco).** O import da ANP mediu um `INSERT ... RETURNING` por linha do EF. Um ano de COTAHIST são ≈ 480 mil linhas: da ordem de minutos, na mesma faixa do ANP. Dia e mês (≈ 2 mil e ≈ 40 mil linhas) não sofrem. Para o carregamento histórico completo (1986 em diante) o caminho certo é o **bulk writer com `COPY`** já decidido em `docs/srs/0002-fuel-anp.md` §10. Se o primeiro slice sair no EF puro, registrar como TDR, com o ponteiro `[TECH-DEBT]` no handler, em vez de deixar implícito.
- **`ExecutionTimeout` do GraphQL.** O limite de 10 minutos do import da ANP precisa ser conferido contra o tempo medido do anual antes de prometer que a mutation serve para ele.
- **Disparo síncrono no request**, igual à ANP. Agendamento (`BackgroundService`) segue em aberto para as duas fontes.
- **Forma do input.** Três campos opcionais em vez de `@oneOf` (§6). Revisar se a validação em runtime incomodar.
- **Preço por lote (`FATCOT`).** Gravar cru e deixar o consumidor dividir (RN-08) é a escolha de menor perda; uma coluna ou propriedade derivada "preço unitário" é fácil de acrescentar quando uma leitura precisar.
- **Mercados fora do escopo.** Opções, termo e demais viriam com valores novos em `QuoteMarketType` e campos próprios (`PREEXE`, `DATVEN`). O PDF documenta a semântica, mas a amostra de hoje não foi conferida para eles.
- **Ajuste por proventos.** Série de preço ajustado exigiria uma fonte de eventos corporativos; não existe no projeto hoje.
- **Relação com a Brapi.** `stockQuote` continua sendo o preço do momento; o COTAHIST só sai depois do fechamento. Não há reconciliação entre as duas fontes.
