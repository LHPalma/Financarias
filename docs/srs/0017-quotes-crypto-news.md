# SRS — Cotação de ações (Brapi), criptoativos (CoinGecko) e notícias (InfoMoney)

- **Data:** 2026-10-11 (escrito depois da entrega, de 04 a 05/07/2026)
- **Feature:** `MarketData/Stocks` (cotação do momento), `MarketData/Cryptos` e `News`
- **Status:** **entregue.** Três leituras em tempo real, sem persistência. Nenhum dado é guardado: cada consulta vai à fonte.
- **Fontes externas:** [Brapi](https://brapi.dev) (ações, com token), [CoinGecko](https://www.coingecko.com) (cripto, sem chave) e o feed RSS do [InfoMoney](https://www.infomoney.com.br).

---

## 1. Introdução

### 1.1 Propósito
Dar ao app móvel (`../financarias-mobile`) e a quem consulta a API o "agora": preço de uma ação, preço de criptoativos e as últimas notícias. É a camada de **tempo real**, e é separada de propósito da base histórica (COTAHIST, `0007`), que só sai depois do fechamento e é guardada.

### 1.2 Escopo
Dentro: `stockQuote`, `cryptoQuotes` (mais os catálogos `availableCryptoAssets` e `availableQuoteCurrencies`) e `latestNews`. Fora: cache, reconciliação com o COTAHIST, histórico, outros ativos e outras fontes de notícia.

## 2. Ações — `stockQuote(ticker)`

- **RN-01 — O ticker é um VO validado** (`Ticker`): uma letra, depois três letras ou dígitos, depois 1 ou 2 dígitos e, opcional, um `F` final (fracionário). Aceita `PETR4`, `PETR4F`, `B3SA3` e `AAPL34`; maiúsculas e minúsculas, e espaços nas pontas, são normalizados. Inválido lança `stock.ticker.invalid`. A raiz com dígito veio depois, junto do COTAHIST (`613863a`), porque o regex de quatro letras recusava 15% do mercado.
- **RN-02 — Inexistente é nulo.** Brapi 404, ou resposta sem preço (`regularMarketPrice` nulo), devolve `null`, com aviso em log.
- **RN-03 — Moeda padrão BRL** quando a fonte não informa; `AsOf` cai para o instante da consulta quando a fonte não traz horário.
- **RN-04 — Token da Brapi é obrigatório no boot** (`Integrations:Brapi:Token`, enviado como `Bearer`); a ausência derruba a aplicação. O valor mora em `user-secrets`/variável de ambiente, nunca no `appsettings.json`.
- Resultado `StockQuoteResult`: símbolo, nome, moeda, preço, variação e variação %, abertura, máxima, mínima, fechamento anterior, volume e instante.

## 3. Criptoativos — `cryptoQuotes(assets, currency)`

- **RN-05 — Catálogo fechado.** `CryptoAsset` tem 8 valores (Bitcoin, Ethereum, Tether, BinanceCoin, Solana, Xrp, Cardano, Dogecoin) e `QuoteCurrency` tem 2 (Brl, Usd). Um catálogo (`CoinGeckoAssetCatalog`) liga cada valor ao identificador da CoinGecko, e um teste garante que **todo valor do enum está no catálogo**, para um ativo novo não passar sem tradução.
- **RN-06 — Quem a fonte não devolve, ou devolve sem preço, fica fora da lista.** Identificador desconhecido é registrado em log e descartado.
- **RN-07 — Lista vazia de ativos devolve lista vazia** sem chamar a fonte.
- Resultado: ativo, símbolo, nome, moeda, preço, valor de mercado, volume, máxima e mínima de 24 h, variação absoluta e % de 24 h, e o instante.

## 4. Notícias — `latestNews(sections, tags)`

- **RN-08 — A fonte é o feed RSS do InfoMoney**, lido com `SyndicationFeed` sobre o fluxo.
- **RN-09 — Seção é a primeira categoria do item; as demais são tags.** `NewsCategory` tem `Mercados`, `Mundo` e `Esportes`.
- **RN-10 — Filtro é a união, não a interseção.** Sem filtro, tudo. Com `sections` e/ou `tags`, a notícia entra se estiver em **alguma** das seções **ou** tiver **alguma** das tags, sem diferenciar maiúsculas de minúsculas.
- **RN-11 — O resumo vem limpo e em HTML.** `summary` é texto puro (tags removidas, entidades decodificadas, espaços colapsados, e a frase "The post … appeared first on InfoMoney" cortada); `summaryHtml` guarda o original. Um resumo que fica vazio vira `null`.

## 5. Camadas
Cada leitura segue UseCase → QueryHandler → porta (`IStockQuoteGateway`, `ICryptoQuoteGateway`, `INewsFeedGateway`) → provedor Refit (`BrapiQuoteProvider`, `CoinGeckoQuoteProvider`, `InfoMoneyNewsProvider`). O `StockQuoteResult`, o `CryptoQuoteResult` e o `NewsArticle` são DTOs da camada de aplicação, documentados em PT-BR para o schema.

## 6. Rastreabilidade
| Commit | Entrega |
|---|---|
| `0d9bd7b`, `8e00941` | `stockQuote` via Brapi e seus testes |
| `d53220b`, `098ebc0` | `cryptoQuotes` via CoinGecko, guarda do catálogo e testes |
| `2a000b5`, `28c2e47`, `ac8e029`, `369e00c`, `aa66cb2` | Notícias: tipos, consulta com filtro, caso de uso, provedor RSS e a query `latestNews` |

## 7. Limites conhecidos
- **Sem reconciliação** entre a Brapi e o COTAHIST: o preço do momento e o fechamento histórico vêm de fontes diferentes.
- **Falha da fonte propaga** como erro (a Brapi só trata 404 como nulo); não há `retry` nem circuito (TDR-011).
- Os limites de uso gratuito da Brapi e da CoinGecko não são tratados; excedê-los vira erro para o cliente.
- O feed do InfoMoney só traz os itens recentes, sem paginação nem arquivo.
