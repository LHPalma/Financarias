# SRS — Câmbio (Foreign Exchange)

- **Data:** 2026-07-06
- **Feature:** `MarketData/ForeignExchange`
- **Status:** entregue e commitado na `main` (7 commits `feat(fx)`), 202 testes verdes
- **Fonte externa:** [er-api](https://www.exchangerate-api.com/docs/free) (`open.er-api.com`, sem chave)

---

## 1. Introdução

### 1.1 Propósito
Especificar os requisitos da feature de **câmbio** do Finançarias: consultar o preço atual de moedas e converter valores entre moedas, expostos via GraphQL. É o **primeiro slice com lógica de domínio de verdade** (uma calculadora de conversão), em contraste com os slices anteriores de market-data que eram passthrough puro (busca → mapeia → devolve).

### 1.2 Escopo
- Conversão de um valor entre duas moedas (`convertCurrency`).
- Preço atual de uma lista de moedas numa moeda de cotação (`currencyPrices`).
- Fonte de cotações on-demand (sem persistência): a er-api.

**Fora de escopo (hoje):** histórico/persistência de cotações, `exchangeRate(from,to)` de par direto (follow-up trivial), moedas fora do enum curado, orquestração multi-fonte + circuit breaker.

### 1.3 Definições
| Termo | Significado |
|---|---|
| **FX** | Foreign Exchange (câmbio). |
| **Pivô / base** | Moeda usada como âncora da triangulação. Aqui, **BRL**. |
| **Quote (cotação)** | Moeda na qual o preço é expresso. Ex.: preço do USD *em BRL*. |
| **Cross-rate / triangulação** | Obter `from→to` passando por uma base comum: `from → base → to`. |
| **Miss** | Ausência de dado (moeda não presente na tabela) — tratado como resultado vazio/`null`, não como erro. |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura (Clean Architecture)
```
Domain/MarketData
├── Currency                    (enum curado, raiz do contexto)
└── ForeignExchange/CurrencyConverter   (calculadora pura)

Application/MarketData/ForeignExchange
├── Gateways/IFxRateGateway             (porta de saída)
├── DTOs/Results/{FxRateSnapshot, ConversionResult, CurrencyPriceResult}
├── Queries/{ConvertCurrency, GetCurrencyPrices}{Query, QueryHandler}
└── UseCases/{IConvertCurrencyUseCase, IGetCurrencyPricesUseCase} (+ impls)

Integrations/MarketData/ErApi
├── Clients/IErApiClient                (Refit)
├── DTOs/Responses/ErApiLatestResponse
└── Providers/ErApiRateProvider         (implementa IFxRateGateway)

Api/GraphQL/Query.cs                     (resolvers convertCurrency, currencyPrices)
```

### 2.2 Fluxo (Estilo A — resolver → UseCase → QueryHandler → resultado)
`GraphQL resolver` → `UseCase` (valida/monta entrada) → `QueryHandler` (busca via porta, aplica regra) → `IFxRateGateway` → `ErApiRateProvider` → er-api.

---

## 3. Requisitos funcionais

### RF-01 — Converter um valor entre moedas
`convertCurrency(amount, from, to, decimals = 2)` retorna o valor convertido.
- Triangula por BRL: `converted = amount × rate[to] / rate[from]`.
- Arredonda `convertedAmount` para `decimals` casas.
- Expõe também a taxa efetiva do par (`rate = rate[to]/rate[from]`) e `asOf`.
- Moeda ausente da tabela ⇒ retorna `null`.

### RF-02 — Preço atual de uma lista de moedas
`currencyPrices(currencies, quote = BRL, decimals = 2)` retorna, para cada moeda pedida, o preço de **1 unidade** dela na moeda `quote`.
- Semântica: **"1 {currency} = {price} {quote}"** (ex.: `EUR/USD = 1.15` ⇒ 1 EUR = 1,15 USD).
- `quote` é opcional (default BRL) — permite preço em USD, EUR, etc.
- Cada preço é arredondado a `decimals` casas.
- Moeda ausente da tabela ⇒ é **pulada** silenciosamente (não quebra a lista).
- Self-quote (`quote == currency`) resulta em `1`.

### RF-03 — Cotações via provedor externo
O sistema obtém as taxas em base BRL da er-api (`GET /v6/latest/BRL`), sem autenticação.

### RF-04 — Vocabulário de moedas curado
As moedas suportadas são um enum fechado exposto no GraphQL: **BRL, USD, EUR, GBP, JPY, ARS**. Os ~160 códigos extras devolvidos pela er-api são ignorados.

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | Triangulação sempre pelo **pivô BRL** (a tabela é buscada em base BRL). |
| **RN-02** | **Arredondamento é apresentação**: mora na camada de aplicação, por parâmetro do client, **default 2 casas**, modo `MidpointRounding.AwayFromZero` (arredondamento comercial). O domínio (`CurrencyConverter`) trabalha em **precisão total**. |
| **RN-03** | **Miss ≠ falha**: moeda ausente da tabela é ausência de dado (`null`/pulada); falha de transporte **propaga** exceção (para futura distinção por circuit breaker). |
| **RN-04** | `Currency → código ISO 4217` via `ToString().ToUpperInvariant()` — acoplamento seguro (padrão internacional), diferente do slug proprietário do CoinGecko. |
| **RN-05** | `CurrencyConverter` é **base-agnóstico**: só exige que `rateFrom` e `rateTo` compartilhem a mesma base. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Testabilidade):** cálculo de conversão isolado numa função pura testável sem I/O; portas mockáveis (NSubstitute).
- **RNF-02 (Anti-corrupção):** o provider traduz o payload externo → DTO da aplicação, iterando o enum `Currency` (não vaza vocabulário do fornecedor à API pública).
- **RNF-03 (Resiliência a dados):** payload snake_case mapeado explicitamente; `rates` ausente tratado como dicionário vazio.
- **RNF-04 (Configuração):** base URL da er-api em `Integrations:ErApi:BaseUrl` (appsettings da API e dos testes de integração — o `AddIntegrations` valida config de todas as integrações no boot).
- **RNF-05 (Consistência de fronteira):** argumentos de lista no GraphQL usam `IReadOnlyList<T>` (o Hot Chocolate **não** materializa `IReadOnlySet<T>`, nem ele é alvo válido de collection-expression `[]`).

---

## 6. Interface — GraphQL

```graphql
type Query {
  convertCurrency(amount: Decimal!, from: Currency!, to: Currency!, decimals: Int! = 2): ConversionResult
  currencyPrices(currencies: [Currency!]!, quote: Currency! = BRL, decimals: Int! = 2): [CurrencyPriceResult!]!
}

enum Currency { BRL USD EUR GBP JPY ARS }

type ConversionResult { from: Currency! to: Currency! amount: Decimal! convertedAmount: Decimal! rate: Decimal! asOf: DateTime! }
type CurrencyPriceResult { currency: Currency! quote: Currency! price: Decimal! asOf: DateTime! }
```

**Exemplos validados ao vivo:**
```graphql
convertCurrency(amount: 1000, from: BRL, to: USD)   # -> convertedAmount 193.11, rate 0.193112
currencyPrices(currencies: [EUR, USD], quote: USD)  # -> EUR: 1.15 (1 EUR = 1,15 USD), USD: 1
```

---

## 7. Integração externa — er-api

- **Endpoint:** `GET https://open.er-api.com/v6/latest/{base}` (base = `BRL`).
- **Auth:** nenhuma.
- **Campos usados:** `base_code`, `time_last_update_unix` (epoch → `DateTimeOffset`), `rates` (`{ "USD": 0.19, ... }`).
- **Falha:** propaga (não engole).

---

## 8. Rastreabilidade (requisito → commit)

| Commit | Entrega |
|---|---|
| `716ef86` | `Currency` + `CurrencyConverter` (triangulação) + teste — RN-01, RN-05 |
| `d3e6d05` | `ConvertCurrency` query/handler + teste — RF-01, RN-02, RN-03 |
| `5fe02f5` | `ConvertCurrency` use case + teste |
| `6314d64` | provider er-api + teste — RF-03, RF-04, RN-04 |
| `ef4a05d` | wiring DI + GraphQL `convertCurrency` + funcional + config |
| `3682fc0` | `GetCurrencyPrices` query/handler + teste — RF-02 |
| `ae59428` | `GetCurrencyPrices` use case + teste |
| `1cc46a1` | wiring DI + GraphQL `currencyPrices` (quote opcional) + funcional |

---

## 9. Cobertura de testes (202 no total, 0 falhas)

| Nível | Cobre |
|---|---|
| Domínio (`CurrencyConverterTests`) | conversão via base; self/base = 1; direção da fórmula (bug de sinal invertido pego aqui). |
| Handler `ConvertCurrency` | conversão via BRL; casas por parâmetro; miss → `null`. |
| Handler `GetCurrencyPrices` | preço em BRL; cotação não-BRL (triangulação); arredondamento; miss pulado. |
| Use cases | delegação + defaults (`quote=BRL`, `decimals=2`). |
| Provider er-api | mapeia só o enum; ignora extras; timestamp unix → `DateTimeOffset`. |
| Funcional (GraphQL) | `convertCurrency` (conversão + null); `currencyPrices` (preço em BRL). |

---

## 10. Follow-ups conhecidos
- `exchangeRate(from, to)` — par direto, mesma gateway (trivial).
- Registro **lazy/por-integração** no `AddIntegrations` (hoje é fail-fast global — qualquer teste que resolve um cliente exige a config de todas as integrações, inclusive segredos).
- Unificar `Currency` (câmbio) e `QuoteCurrency` (cripto) num enum único do contexto MarketData.
