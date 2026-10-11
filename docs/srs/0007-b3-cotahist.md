# SRS — Cotações históricas da B3 / Import COTAHIST (Stocks)

- **Data:** 2026-10-10
- **Feature:** `MarketData/Stocks` (histórico) + `Integrations/MarketData/B3/Cotahist`
- **Status:** **entregue, exceto a consulta.** O import inteiro (domínio, parser, provider, handler, persistência, caso de uso e a mutation `importDailyQuotes`) está em `main` (§8) e já carregou de 02/01/1986 a 09/10/2026, 7,21 milhões de linhas (§11), com as moedas antigas guardadas como publicadas e convertidas para reais na leitura (RN-15 e ADR-002). Falta a query de leitura `dailyQuotes` (RF-08). **Revisado em 2026-10-11** para refletir o que foi construído: onde este texto diverge da especificação original, vale o que está aqui, e as mudanças mais relevantes são a porta em dois passos (RF-02/03), o `@oneOf` no input (§6), a conversão de códigos antigos (RN-15 a RN-18) e a colisão de chave do `BIOM12F` (RN-02).
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
- Disparo manual via mutation GraphQL `importDailyQuotes` (entregue). A leitura via query `dailyQuotes` **não existe ainda**: hoje os dados se leem direto no banco.
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
├── Import/{IDailyQuoteProvider, IDailyQuoteFile, DailyQuoteImportItem, DailyQuoteImportResult}
├── Specifications/QuotesByTickersAndDatesSpecification
├── Commands/{ImportDailyQuotesCommand, ImportDailyQuotesCommandHandler}
├── DTOs/Requests/{ImportDailyQuotesRequest, QuoteMonthRequest}
├── UseCases/{IImportDailyQuotesUseCase + impl}
└── Queries/{IStockReads, StockReads}   (NÃO implementado: leitura Style B, quando a query chegar)

Infrastructure/Persistence
├── Configurations/DailyQuoteConfiguration
├── Converters/TickerConverter
└── Migrations/{AddDailyQuotes, AddLegacyTickerToDailyQuotes}

Integrations/MarketData/B3/Cotahist
├── Clients/IB3CotahistClient          (Refit, devolve Stream)
├── CotahistLineParser + CotahistLineResult   (puro: string de 245 chars → item, miss ou fora de escopo)
└── Providers/{B3CotahistProvider, CotahistFile}   (provider abre o ZIP; CotahistFile lê em streaming)

Api/GraphQL
├── Mutations/StockMutations           (importDailyQuotes)
└── Types/ImportDailyQuotesInputType   (@oneOf do período)

NÃO implementado: Queries/StockQueries (dailyQuotes) e Types/DailyQuoteType.
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
A porta é **em dois passos**, e não um simples `IAsyncEnumerable`: `IDailyQuoteProvider.OpenAsync(período)` devolve um `IDailyQuoteFile?` (nulo quando o arquivo não existe), que é `IAsyncDisposable` e expõe `ReadAsync()`, um `IAsyncEnumerable<DailyQuoteImportItem>` item a item, mais `RowsSkipped`, que só fica completo depois de percorrer o arquivo. Os dois passos existem porque o arquivo temporário do ZIP precisa ser apagado ao fim (RNF-01) e o contador de linhas puladas só se sabe no fim da leitura.

### RF-04 — Traduzir o registro `01` para o modelo de domínio
Mapear os campos posicionais para `Ticker`, `DateOnly`, `QuoteMarketType`, `decimal`, `int` e `long`.

### RF-05 — Persistir com upsert por chave natural
Chave `(ticker, data do pregão, tipo de mercado)`: insere novas, atualiza as que mudaram, ignora idênticas.

### RF-06 — Reportar o resultado do import
Devolver o que foi lido, criado, atualizado e pulado, e se o arquivo existia.

### RF-07 — Disparo manual via GraphQL
Mutation `importDailyQuotes`. **Entregue.**

### RF-08 — Consultar o histórico
Query `dailyQuotes` com filtro, ordenação e paginação (por ticker e por intervalo de datas, no mínimo). **Não implementado.**

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | **Um agregado, uma linha por papel-mercado-pregão.** `DailyQuote` é raiz de agregado sem filhos. O ticker é uma `string`/VO, não há entidade de papel: só o dado que o arquivo traz. |
| **RN-02** | **Chave natural `(ticker, data do pregão, tipo de mercado)`**, garantida por índice único. Na amostra de um único dia (2026-10-10) não havia duplicata nas 1.847 linhas à vista, e incluir o `CODBDI` na chave também não gerava. **Numa amostra maior isso deixou de valer:** em novembro de 2022 (33.339 linhas à vista e fracionárias) apareceram **5 chaves repetidas**, todas as inspecionadas do `BIOM12F` no fracionário, onde duas espécies diferentes (`BNS ORD MA`, ISIN `BRBIOMN03OR2`, e `ON  REC MA`, ISIN `BRBIOMR12OR4`) negociam sob o mesmo código. O import trata a segunda como atualização da primeira (RN-12), então **uma das duas linhas se perde**. **Decisão em aberto:** incluir o ISIN na chave, o que separaria as espécies, ao custo de um índice maior e de mudar a chave única. Enquanto não se decide, o efeito é raro (poucas linhas por mês, fracionário) e conhecido. O `CODBDI` continua fora da chave: classifica, não identifica. |
| **RN-03** | **O ISIN (`CODISI`) é guardado**, mas não há FK nem entidade de instrumento. Todas as linhas à vista da amostra trazem ISIN começando com `BR`. É o gancho para ligar ao emissor quando esse modelo existir. |
| **RN-04** | **Filtro por `TPMERC`, não por `CODBDI`.** Só 010 e 020 entram. Filtrar por BDI quebraria: a amostra tem 11 BDIs diferentes no mercado à vista, e o PDF não documenta 4 deles (13, 34, 35 e 36). Opções, termo e demais mercados ficam como extensão: acrescentar valores a `QuoteMarketType` e liberar o filtro. |
| **RN-05** | **`CODBDI` é guardado como texto de 2 caracteres**, sem enum. A tabela do PDF está defasada em relação ao arquivo (RN-10). |
| **RN-06** | **Miss ≠ falha.** Linha com tamanho diferente de 245, tipo de registro desconhecido, ticker que não casa com `Ticker`, data ilegível, número ilegível, moeda diferente de `R$` ou violação da RN-07 é **pulada**, contada em `rowsSkipped` e logada. Falha de transporte (rede, 5xx, ZIP corrompido) **propaga**. **404 é caso esperado:** fim de semana, feriado e data futura não têm arquivo diário (verificado: `D10102026`, `D12102026`, `D07092026` e `D12102025` respondem 404). |
| **RN-07** | **Invariantes do agregado:** abertura, máxima, mínima, média e fechamento **> 0**; máxima ≥ mínima; negócios > 0; quantidade > 0. Valem no `Create`/`UpdateValues` **e** como `CHECK` no banco, para sobreviverem a caminhos que contornem o agregado. O agregado expõe `DailyQuote.GetFiguresError`, que o parser usa para pular a linha como miss sem lançar exceção; o `Create` lança `stock.quote.invalid` pelo mesmo caminho. Nenhuma linha à vista da amostra viola isso. **Não há invariante sobre a média:** em 1 linha de 1.847 o `PREMED` caiu fora do intervalo [mínima, máxima]. |
| **RN-08** | **Preço é gravado como publicado, com o fator de cotação ao lado.** O `FATCOT` diz se o preço vale por ação (`1`) ou por lote (`100`, `1000`). Na amostra: 1.844 linhas com `1`, 2 com `1000` e 1 com `100`. Guardar o dado cru preserva a fidelidade à fonte; quem consome divide pelo fator. Os preços **não têm ajuste** por desdobramento, grupamento ou proventos (a própria B3 avisa), então uma série longa tem saltos nessas datas. É limitação conhecida, não bug. |
| **RN-09** | **O VO `Ticker` aceita dígito na raiz** (`^[A-Z][A-Z0-9]{3}\d{1,2}F?$`). O regex antigo, de 4 letras, rejeitava 15% do mercado à vista (279 de 1.847): `B3SA3`, BDRs (`V1IP34`, `BPIC39`) e ETFs (`B1003`). Com o novo, só `MRSA3BF` fica de fora (fracionário com sufixo de classe) e vira miss. A mudança vale também para `stockQuote` (Brapi). **Entregue** em `613863a`. |
| **RN-10** | **O arquivo é a fonte de verdade, não o PDF.** Divergências verificadas estão em §7.3. O parser não depende da tabela de BDI nem do conjunto de fatores de cotação do PDF. |
| **RN-11** | **Import é idempotente.** Reexecutar é seguro e é o mecanismo de recuperação. Reimportar o ano corrente (que a B3 reconstrói todo dia) atualiza só o que mudou. |
| **RN-12** | **Read-before-write via repositório**, nunca pelo contrato de leitura (mesma regra do import da ANP). O handler carrega os agregados existentes por `IRepository<DailyQuote>` + `Specification` e muda pelos métodos do agregado. |
| **RN-13** | **O trailer é conferência, não bloqueio.** Divergência entre o total do trailer e o número de registros `01` lidos gera log de aviso, e o import segue. Ver §7.3 sobre o que o trailer conta de fato. Na carga histórica os anuais de 1986 a 1994, 1998 e 2000 avisaram divergência de **exatamente 2 registros** (por exemplo 119.099 declarados contra 119.097 lidos em 1994, e 104.973 contra 104.971 em 1998). Uma diferença constante de 2 em onze arquivos é estrutural: **nesses arquivos o trailer conta também o header e o próprio trailer**, como diz o PDF, ao contrário do diário de 2026, que só conta os registros `01` (§7.3). O import seguiu como previsto, e o aviso desses anos não indica linha perdida. |
| **RN-14** | **Período inválido é erro de domínio.** Lança `stock.quoteperiod.invalid` o pedido que não é exatamente um dia, um mês ou um ano (nenhum campo, ou mais de um), o mês fora de 1–12 e o ano ou a data **anteriores a 1986**. A validação fica em `QuotePeriod` e no use case, antes de qualquer download. **Não há limite superior:** data futura ou ano futuro passam e terminam em `fileFound: false`, porque a B3 responde 404 (RN-06). (A versão original deste documento prometia recusar o futuro; não foi construído, e o 404 já dá o mesmo resultado sem erro.) |
| **RN-15** | **A moeda da linha tem de ser a que vigorava no dia; o valor é gravado na moeda original.** Os arquivos anteriores a 01/07/1994 trazem Cruzeiro, Cruzado, Cruzado Novo e Cruzeiro Real (`CR$`, `CZ$`, `NCZ$`). O parser aceita a linha se o código do campo `MODREF` for o da moeda daquela **data** (`BrazilianCurrency.IsLegalTenderOn`) e pula a que divergir, como `R$` em 1993 ou `CR$` em 2026. A moeda vem da data porque `CR$` é ambíguo: nomeia três moedas de valores diferentes. Os preços e volumes ficam **como a B3 publica**, e a conversão para reais é feita na leitura, por `BrazilianCurrency.ToReais(valor, data)` ou pela função `to_reais(valor, data)` do Postgres. A decisão e as alternativas rejeitadas estão no `docs/adr/0002-original-currency-converted-on-read.md`. O primeiro pregão do banco passou a ser **02/01/1986**. |
| **RN-16** | **Códigos antigos são convertidos para o ticker atual.** Até 1997 os códigos tinham três letras e um espaço de preenchimento (`PET 4`, `BBD 4`, `ITA 4`, `BB  4`). O parser reconstrói o ticker pela **raiz do ISIN** (posições 3–6, depois do `BR`) mais o dígito da classe: `PET 4` → `PETR4`. Em 1995 as duas formas convivem no mesmo arquivo: de janeiro a abril o campo do ISIN traz o código antigo da empresa (`ACESACON`) em vez do ISIN, e a raiz são as **quatro primeiras posições**. Em 1995, 79% das linhas derivam para um ticker que existe em 1999–2001; o resto são empresas que já tinham saído, e o código derivado continua com formato válido. **Limite conhecido:** quando a classe mudou de número, o derivado não existe depois (`VAL 4` → `VALE4`, que na série moderna é `VALE5`). Uma tabela de exceções não foi construída. |
| **RN-17** | **O código original é guardado em `legacy_ticker`.** Coluna `varchar(12)` nula, preenchida só quando o parser converteu (RN-16 e RN-18), com índice parcial `WHERE legacy_ticker IS NOT NULL`. Serve para pesquisar uma cotação pelo nome com que ela era negociada. Fica nula para todo código moderno. |
| **RN-18** | **Sufixo duplo do fracionário é reduzido.** Em 2000 o mercado fracionário traz `PETR4FF`, com dois `F`; o `Ticker` aceita só um. O parser reduz para `PETR4F` e guarda o original em `legacy_ticker`. Eram 47.076 linhas de 2000, puladas até esta regra existir. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Memória):** o ZIP anual tem ~91 MB e expande para bem mais de 1 GB (≈ 4,8 milhões de linhas, só ~10% à vista). O download vai para um **arquivo temporário** (`FileOptions.DeleteOnClose`), porque `ZipArchive` exige stream seekable e bufferizar 91 MB em `MemoryStream` não escala. A entrada é lida em **streaming**, linha a linha, sem materializar o TXT.
- **RNF-02 (Change tracker):** `IUnitOfWork.ClearTracking()` ao fim de cada lote de 5.000 (mesma razão do import da ANP).
- **RNF-03 (Anti-corrupção):** o parser lê por **posição** e traduz para tipos de domínio; o vocabulário da B3 (`TPMERC`, `CODBDI`) não vaza para além do provider e do enum curado.
- **RNF-04 (Formato do dado):** largura fixa, 245 caracteres por linha, terminador CRLF, **sem separador**. Preços são inteiros com 2 casas implícitas (`/ 100`), volume idem. Datas em `AAAAMMDD`. Codificação ANSI do Windows: ler como **Latin1**; as diferenças para a Windows-1252 estão só em 0x80–0x9F e não tocam ticker, número ou data.
- **RNF-05 (Configuração):** `Integrations:B3:BaseUrl` (padrão `https://bvmf.bmfbovespa.com.br`) no appsettings da API e dos testes, como a ANP. O `AddIntegrations` valida a configuração de todas as integrações no boot.
- **RNF-06 (Timeouts):** `HttpClient` da B3 com timeout de 10 minutos (o anual recente tem ~90 MB). Os imports anuais de até ~414 mil linhas terminaram pela própria mutation, sem estourar o `ExecutionTimeout` do Hot Chocolate; o limite não foi levado ao extremo.
- **RNF-07 (Testabilidade):** o `CotahistLineParser` é função pura, testada com **linhas literais copiadas do arquivo real**; o provider, com client substituído e ZIP montado em memória; o handler, sem banco (portas substituídas) e **com** banco real (Testcontainers).
- **RNF-08 (Tamanho medido):** de 90 a 110 mil linhas à vista por ano nos anos 1990 e até 2006, e de 156 mil (2010) a 435 mil (2025) depois, contra a estimativa original de ≈ 480 mil por ano. Os arquivos anuais de 1986 em diante existem (`A1986` responde 200, 8,7 MB), mas só os de julho de 1994 em diante têm linhas em reais (RN-15).
- **RNF-09 (Velocidade de escrita):** medida em ≈ 1 ms por linha, pelo EF linha a linha: um mês (≈ 25 mil linhas) leva em torno de 25 s, e o anual de 2008 (165.527 linhas) levou 153 s. O ano recente de 400 mil linhas leva alguns minutos. É a faixa esperada no risco de §10.

---

## 6. Interface — GraphQL

**Entregue** (`StockMutations` e `ImportDailyQuotesInputType`):

```graphql
type Mutation {
  "Importa as cotações diárias da B3 (série histórica COTAHIST) de um dia, de um mês ou de um ano. Público e pesado: qualquer um pode chamar hoje."
  importDailyQuotes(input: ImportDailyQuotesRequestInput!): DailyQuoteImportResult!
}

input ImportDailyQuotesRequestInput @oneOf {
  "Um pregão específico, como 2026-10-09."
  day: Date
  "Um mês inteiro."
  month: QuoteMonthRequestInput
  "Um ano inteiro, a partir de 1986."
  year: Int
}

input QuoteMonthRequestInput { year: Int!, month: Int! }

type DailyQuoteImportResult {
  fileFound: Boolean!
  rowsRead: Int!
  rowsSkipped: Int!
  quotesCreated: Int!
  quotesUpdated: Int!
}
```

O input usa **`@oneOf`** (`descriptor.OneOf()`): exatamente um entre `day`, `month` e `year`. Isso **substitui** a ideia original de três campos opcionais com validação em runtime. Um mês é um objeto `{ year, month }`, e não um par solto de campos, porque assim `month` nunca aparece sem o ano. Mais de um campo, ou nenhum, é recusado pelo schema com HTTP 400, antes de executar (verificado por teste funcional).

**Não implementado:** `type Query { dailyQuotes(...): DailyQuoteConnection }` (Estilo B: `where`/`order`/paginação).

Quando a query chegar, `DailyQuote` fica exposto direto, por um `ObjectType<DailyQuote>` com `BindFieldsExplicitly()`, como `FuelPriceType`. **O VO `Ticker` precisa ser declarado a partir do membro** (`descriptor.Field(q => q.Ticker).Name("ticker").Type<NonNullType<StringType>>().Resolve(...)`): declarar com `Field("ticker").Resolve(...)` compila, passa nos testes unitários e falha em runtime com `Unexpected Execution Error`, porque a projeção do EF não vê a coluna (CLAUDE.md, *Persistence*). Só um funcional contra banco real pega isso.

Toda a superfície nasce documentada em XML em PT-BR (regra da documentação do contrato GraphQL), incluindo que `importDailyQuotes` é público e pesado.

**Erros (`extensions.code`):** `stock.quoteperiod.invalid` (período), `stock.quote.invalid` (invariante do agregado, só alcançável por caminho que contorne o parser).

---

## 7. Integração externa — B3

### 7.1 Endpoint
- **URL:** `GET https://bvmf.bmfbovespa.com.br/InstDados/SerHist/{arquivo}`, onde `arquivo` é `COTAHIST_A{yyyy}.ZIP`, `COTAHIST_M{MMyyyy}.ZIP` ou `COTAHIST_D{ddMMyyyy}.ZIP`. O domínio `www.b3.com.br/pb/inst-dados/...` responde 404; este é o host que serve o arquivo.
- **Auth:** nenhuma.
- **Disponibilidade medida em 2026-10-10:** anual de 1986 (8,7 MB) a 2026 (91 MB, reconstruído diariamente); mensal de 2020, 2024 e 2026 (3 a 10 MB); diário de 02/09/2025 (332 KB) a 09/10/2026 (564 KB), sem que se tenha testado o limite de retenção. Dias sem pregão e datas futuras: 404.
- **Conteúdo do ZIP:** um único arquivo de texto. Nos arquivos de 2002 em diante ele se chama `COTAHIST_D09102026.TXT` (ou `_M`/`_A`); **nos anuais até 2001 o nome não tem extensão** (`COTAHIST.A1995`). O provider escolhe a entrada cujo nome **começa por `COTAHIST`**, sem depender da extensão. A primeira versão procurava `.TXT` e falhou em todo ano anterior a 2002 (corrigido em `e1f0d1a`).
- **Importado de fato:** a cobertura por ano está em §11. Anuais e mensais foram misturados conforme o ano, porque o mensal só existe para os anos recentes.
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
6. **A entrada do ZIP perde a extensão `.TXT` nos anuais até 2001** (`COTAHIST.A1995`), ver §7.1.
7. **O campo `CODISI` nem sempre é um ISIN.** Em 1995, de janeiro a abril, traz o código antigo da empresa (`ACESACON`); de maio em diante, o ISIN completo (`BRACESACNOR5`). Ver RN-16.
8. **Códigos de negociação de 3 letras com espaço até 1997** (`PET 4`, `BB  4`), e **sufixo duplo `FF`** no fracionário em 2000 (`PETR4FF`). Ver RN-16 e RN-18.
9. **Moeda diferente de `R$` antes de 01/07/1994**, ver RN-15.
10. **O trailer dos anuais antigos conta header e trailer.** Nos anuais de 1986 a 1994, 1998 e 2000 o total declarado é sempre o dos registros `01` mais 2, o que difere do diário de 2026 (ponto 3). Não foi verificado em que ano a convenção muda (RN-13).

Um sexto ponto é de contexto, não de divergência: a revisão do PDF é de 2020, então o conferido hoje vale como o contrato real. O teste de contrato externo (§9) existe justamente para avisar quando o formato mudar.

---

## 8. Rastreabilidade (requisito → commit)

| Commit | Entrega |
|---|---|
| `613863a` | `Ticker` aceita dígito na raiz — RN-09 |
| `5c89a35` | `QuotePeriod` (dia, mês e ano) — RF-01, RN-14 |
| `30eb79f` | Agregado `DailyQuote` e `QuoteMarketType` — RN-01, RN-07 |
| `2b2c2a9` | Porta `IDailyQuoteProvider` e item de importação — RF-03 |
| `abba523` | `GetFiguresError`, validação sem exceção usada pelo parser — RN-07 |
| `da1c409` | `CotahistLineParser` — RF-04, RN-03 a RN-06, RN-08 |
| `33035d0` | Comando e handler com upsert em lotes — RF-05, RF-06, RN-11, RN-12, RNF-02 |
| `2633dbd` | `B3CotahistProvider`, download em arquivo temporário e leitura em streaming — RF-02, RNF-01, RN-13 |
| `bb3f13d` | Persistência com índice único e `CHECK` — RN-02, RN-07 |
| `657160e` | Caso de uso `ImportDailyQuotes` — RF-01 |
| `f12c5a2` | Mutation `importDailyQuotes` com input `@oneOf` — RF-07, §6 |
| `e1f0d1a` | Entrada do ZIP achada por nome, não por `.TXT` — §7.1 |
| `d14c688` | Coluna `legacy_ticker` — RN-17 |
| `2bfe4ce` | Conversão de códigos antigos e do sufixo `FF` — RN-15, RN-16, RN-18 |
| (pendente) | Query `dailyQuotes` — RF-08 |

---

## 9. Cobertura de testes

| Nível | Cobre | Status |
|---|---|---|
| Domínio (`TickerTests`) | raiz com dígito (`B3SA3`, `AAPL34`, `V1IP34`, `B1003`) e entradas inválidas novas. | Entregue |
| Domínio (`QuotePeriodTests`, `DailyQuoteTests`) | período válido e inválido por forma; `Create` e `UpdateValues` revalidam RN-07; código do erro como literal. | Entregue |
| Parser (`CotahistLineParserTests`) | linhas literais copiadas do arquivo real: ação comum, BDR, ETF, fracionário, `FATCOT` 100 e 1000; preço `/100`; **miss** para tamanho errado, ticker fora do padrão, mercado fora de escopo, moeda ≠ `R$`, número ilegível; e as conversões de **códigos antigos** de 1995 (`PET 4`, `BB  4`, as duas formas de ISIN) e do `PETR4FF` de 2000. | Entregue |
| Provider (`B3CotahistProviderTests`) | ZIP montado em memória; header e trailer ignorados; aviso quando o trailer diverge; 404 vira "arquivo não encontrado"; falha de transporte propaga; entrada sem `.TXT` (`COTAHIST.A1995`). | Entregue |
| Handler e caso de uso (unit) | cria; atualiza o alterado; ignora o idêntico; chave repetida no lote vira uma; limpa o tracker por lote; período inválido não chega ao handler. | Entregue |
| Persistência (Testcontainers) | round-trip com o VO `Ticker`; índice único barrando duplicata; `CHECK` barrando preço ≤ 0; `legacy_ticker` gravado e pesquisável; upsert e2e em `DbContext`s separados (`ImportDailyQuotesHandlerTests`). | Entregue |
| Funcional (`ImportDailyQuotesMutationTests`) | dia, mês e ano chegam ao provider; arquivo inexistente devolve zeros; `@oneOf` recusa mais de um campo e input vazio; ano anterior a 1986 devolve `stock.quoteperiod.invalid`; o schema declara `@oneOf`. | Entregue |
| Funcional da query `dailyQuotes` | `where`/`order`/paginação **contra banco real** (pega o problema do VO na projeção). | Pendente (a query não existe) |
| Contrato externo (`external-contracts.yml`) | baixa um diário real recente e confere: 245 caracteres por linha, header e trailer presentes, parse sem miss inesperado. Fora do CI normal, pelo mesmo motivo do ViaCEP e da Anbima. | **Pendente** |

---

## 10. Riscos e decisões em aberto

- **Performance da escrita (medida).** O EF faz um `INSERT ... RETURNING` por linha: ≈ 1 ms por linha (RNF-09). A carga de 1994 a 2026 passou, com um ano recente de 400 mil linhas levando alguns minutos. É viável para carga manual e lenta para reconstruir tudo. O **bulk writer com `COPY`** decidido em `docs/srs/0002-fuel-anp.md` §10 **não foi construído**, e o TDR que este documento prometia **não foi aberto**: o handler não tem o ponteiro `[TECH-DEBT]`. É dívida assumida sem registro.
- **`ExecutionTimeout` do GraphQL.** Resolvido para o uso atual: imports anuais de até ~414 mil linhas terminaram pela mutation. O limite exato não foi medido.
- **Disparo síncrono no request**, igual à ANP. Agendamento (`BackgroundService`) segue em aberto para as duas fontes.
- **Colisão de chave (`BIOM12F`).** Decisão sobre incluir o ISIN na chave única em aberto (RN-02).
- **Exceções de código antigo.** A conversão pela raiz do ISIN erra quando a classe mudou de número (`VAL 4` → `VALE4`, hoje `VALE5`). Uma tabela de exceções não existe (RN-16).
- **Emenda de tickers que mudaram por fusão.** O Itaú muda de `ITAU4` (até 19/05/2009) para `ITUB4` (a partir de 20/05/2009), por causa da fusão com o Unibanco. A série contínua exige emendar os dois códigos, e não é só renomear: houve troca de ações. Não implementado.
- **Query `dailyQuotes` e contrato externo.** Pendentes (RF-08 e §9).
- **Preço por lote (`FATCOT`).** Gravar cru e deixar o consumidor dividir (RN-08) é a escolha de menor perda; uma coluna ou propriedade derivada "preço unitário" é fácil de acrescentar quando uma leitura precisar.
- **Mercados fora do escopo.** Opções, termo e demais viriam com valores novos em `QuoteMarketType` e campos próprios (`PREEXE`, `DATVEN`). O PDF documenta a semântica, mas a amostra de hoje não foi conferida para eles.
- **Ajuste por proventos.** Série de preço ajustado exigiria uma fonte de eventos corporativos; não existe no projeto hoje.
- **Relação com a Brapi.** `stockQuote` continua sendo o preço do momento; o COTAHIST só sai depois do fechamento. Não há reconciliação entre as duas fontes.

---

## 11. Carga histórica (estado em 2026-10-11)

| Item | Valor |
|---|---|
| Período | **02/01/1986 a 09/10/2026** (RN-15: as moedas anteriores a 07/1994 ficam como publicadas) |
| Linhas em `daily_quotes` | **7.212.134**: 4.736.245 à vista e 2.475.889 fracionárias |
| Tamanho | 1.725 MB: 1.062 MB de dados e 663 MB de índices |
| Pregões por ano | de 240 a 251 em cada ano completo; 1994 tem 245 (toda a metade em Cruzeiro Real mais a metade em reais) e 2026 tem 194 (até 09/10) |
| Linhas com `legacy_ticker` | **1.443.225**: os códigos de três letras de 1986 a 1998, mais 47.076 do sufixo `FF` de 2000 |
| Lacunas | nenhum mês ausente de janeiro de 2002 a outubro de 2026 (conferido mês a mês); de 1986 a 2001 a conferência foi por ano |

**Validação cruzada.** O número de pregões de cada ano em `daily_quotes` coincide, ano a ano, com o número de pregões do Ibovespa oficial em `index_quotes` (`docs/srs/0008-ibovespa-index.md`, §11), de julho de 1994 em diante: nenhum pregão de um lado sem o outro. A conferência não foi refeita para 1986 a junho de 1994.

### 11.1 Moedas antigas (1986 a junho de 1994)

| Arquivo | Linhas gravadas | Puladas |
|---|---|---|
| 1986 | 128.723 | 3.776 |
| 1987 | 116.077 | 2.820 |
| 1988 | 156.513 | 3.067 |
| 1989 | 144.904 | 2.877 |
| 1990 | 105.028 | 1.557 |
| 1991 | 105.538 | 1.548 |
| 1992 | 100.436 | 1.337 |
| 1993 | 110.618 | 449 |
| 1994 (só o que faltava, em Cruzeiro Real) | 57.046 | 62 |

Total de **1.024.883** linhas novas, e o banco passou de 6.187.251 para 7.212.134.

**A conversão foi conferida nos dados**, e não só nas leis. Em cada troca de moeda, a mediana entre os papéis negociados nos dois pregões em volta da troca da razão entre o preço por ação em reais (`to_reais(close) ÷ quote_factor`) é de ordem 1, sem nenhum salto de 1000×:

| Troca | Pregões | Papéis | Razão (mediana) |
|---|---|---|---|
| Cruzeiro → Cruzado | 27/02 → 04/03/1986 | 266 | 1,200 |
| Cruzado → Cruzado Novo | 13/01 → 18/01/1989 | 194 | 0,968 |
| Cruzado Novo → Cruzeiro | 13/03 → 19/03/1990 | 2 | 0,714 |
| Cruzeiro → Cruzeiro Real | 30/07 → 02/08/1993 | 181 | 0,998 |
| Cruzeiro Real → Real | 30/06 → 04/07/1994 | 136 | 1,002 |

As duas últimas, com mais papéis e quartis apertados (0,964 a 1,000 e 0,974 a 1,038), são a prova mais forte. As três primeiras trazem a variação dos planos econômicos (o Cruzado, o Verão e o Collor): a bolsa ficou fechada alguns dias em volta de cada um e a de 1990 tem só dois papéis negociados nos dois lados, então a evidência ali é de ordem de grandeza, não de precisão.

**Saltos que a conversão não explica.** Em oito papéis líquidos de 1986 a 1994, 20 variações diárias foram maiores que 2,5× ou menores que 0,4× depois da conversão, sem desdobramento, grupamento ou bonificação correspondente na B3. Alguns são de ~1000× e caem no mesmo dia em papéis diferentes (BBDC4 e ITAU4 em 01/06/1987), o que aponta para mudança de lote de cotação sem o `quote_factor` acompanhar, e os demais para eventos que a B3 não publica para o período. **Antes de usar a série de 1986 a 1994 em retorno, ela precisa da auditoria de saltos** do slice de preço ajustado (`docs/srs/0010-corporate-events.md`, RN-08).

**Como foi carregado.** Por anual onde o mensal não existe (os anos 1990 e 2000, e alguns dos 2010) e por mensal nos anos recentes. Reimportar é seguro (RN-11), e foi o que corrigiu 1998 e 2000 depois de cada ajuste do parser: o contador `quotesCreated` mostrou o que o ajuste destravou. Os totais acima são contagens exatas feitas em 2026-10-11, depois da última importação.
