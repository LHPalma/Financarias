# SRS — Indicadores econômicos do Banco Central (dólar, Selic, CDI, IPCA)

- **Data:** 2026-10-11
- **Feature:** `MarketData/Indicators` + `Integrations/MarketData/Bcb/Sgs`
- **Status:** **entregue, exceto a consulta.** Domínio, tabela `indicator_values`, importação e a mutation `importIndicatorValues` estão em `main` (§7). Falta a query de leitura (§5). Primeira carga feita (§8).
- **Fonte externa:** API de séries temporais do Banco Central (SGS), `https://api.bcb.gov.br/dados/serie/bcdata.sgs.{série}/dados`, pública e sem chave.
- **Base empírica:** as quatro séries foram conferidas em **2026-10-11** direto na API, com os valores mais recentes e o primeiro ano com dados de cada uma.

---

## 1. Introdução

### 1.1 Propósito
Guardar o contexto macroeconômico de cada pregão: o **dólar**, para mostrar o Ibovespa em dólar e separar efeito local de câmbio; a **Selic** e o **CDI**, o custo do dinheiro; e o **IPCA**, a inflação. Alimenta os estudos de evento (eleições, crises) com o "o que mais mexia na economia naquele dia".

### 1.2 Escopo
- Quatro indicadores, um valor por indicador e data de referência.
- Importação por **ano**, idempotente, disparada por mutation GraphQL.

**Fora de escopo (hoje):**
- Outros indicadores e moedas (euro, IGP-M, PIB, juros futuros).
- Ajuste ou conversão do dólar anterior ao Real (RN-03).
- Datas de reunião do Copom como evento (o histórico da meta Selic já mostra cada mudança).

## 2. As séries

| Indicador | Série SGS | O que é | Frequência | Primeiro dado |
|---|---|---|---|---|
| `Dollar` | 1 | Dólar comercial, venda, R$ por US$ | dias úteis | 28/11/1984 |
| `SelicTarget` | 432 | Meta Selic definida pelo Copom, % a.a. | todo dia do calendário | 05/03/1999 |
| `Cdi` | 4389 | CDI anualizado, base 252, % a.a. | dias úteis | 06/03/1986 |
| `Ipca` | 433 | Variação mensal do IPCA, % | mensal, data no dia 1º | 01/01/1980 |

A série 10813, que também devolve o dólar, traz valores levemente menores no mesmo dia (5,0113 contra 5,0119 em 08/10/2026), provavelmente a cotação de compra, e não foi usada.

## 3. Regras de negócio

- **RN-01 — Identidade.** Uma linha por indicador e data de referência (`ix_indicator_values_indicator_reference_date`, único). A chave primária é um `id` `bigint` comum, como em `index_quotes` e `daily_quotes`.
- **RN-02 — Sem restrição de sinal.** O valor pode ser negativo (IPCA em deflação: −0,32% em agosto de 2026) ou zero, então não há `CHECK` de positivo. Seis casas decimais cobrem a cotação do dólar (4) e as taxas diárias.
- **RN-03 — Série bruta, na moeda da época.** O dólar anterior ao Real está na moeda daquele tempo, sem conversão: 2.828,00 em 28/11/1984, 2.750,00 em 30/06/1994 e **1,00 em 01/07/1994**. Não se compara nem se calcula variação do dólar atravessando 01/07/1994. De julho de 1994 em diante a série é contínua. É o mesmo cuidado do corte de base do Ibovespa (`0008`, RN-08).
- **RN-04 — Idempotência.** Reimportar um ano não duplica: igual é ignorado, diferente é atualizado, ausente é criado.
- **RN-05 — Datas futuras não são gravadas.** O Banco Central já publica a meta Selic adiante (até 04/11/2026, na consulta de 11/10/2026). O provider descarta o que for posterior ao dia de hoje; importar de novo mais tarde completa o ano.
- **RN-06 — Ano sem dados é vazio, não erro.** A API devolve HTTP 404 para uma janela sem valores (por exemplo, o dólar em 1980); o provider trata como lista vazia e a importação devolve zero linhas.
- **RN-07 — Calendários diferentes.** A meta Selic existe todo dia, inclusive fim de semana (10.083 valores desde 1999). O dólar e o CDI só em dia útil. O CDI chega com um dia de atraso em relação ao dólar. Quem junta com o pregão deve usar o dólar e a Selic do mesmo dia e tolerar o CDI do dia útil anterior.

## 4. Requisitos funcionais

| ID | Requisito | Status |
|---|---|---|
| RF-01 | Persistir o valor de um indicador numa data. | Entregue |
| RF-02 | Importar do Banco Central todos os valores de um indicador num ano, de forma idempotente. | Entregue |
| RF-03 | Expor a importação por mutation GraphQL. | Entregue |
| RF-04 | Consultar os valores por indicador e intervalo de datas. | Não implementado |

## 5. Superfície GraphQL

- **Entregue:** `importIndicatorValues(input: { indicator: DOLLAR, year: 2018 })` devolve `rowsRead`, `valuesCreated` e `valuesUpdated`. Erro de domínio: `indicator.year.invalid` para ano anterior a 1980. Hoje não exige autenticação, como as demais mutations de importação.
- **Planejado:** a query de leitura (RF-04), Estilo B, junto com a do índice.

## 6. Riscos

- **Falhas passageiras do Banco Central.** Na primeira passada, 6 das 159 chamadas falharam (quatro `502 Bad Gateway` e dois erros de leitura da resposta) e todas passaram na segunda tentativa. O cliente não tem política de nova tentativa. Mitigação futura: resiliência no `HttpClient` (`Microsoft.Extensions.Http.Resilience`), prevista no projeto para os clientes externos.
- **Dólar anterior ao Real.** Ver RN-03; quem consome precisa respeitar o corte.

## 7. Rastreabilidade

| Requisito / regra | Commit |
|---|---|
| RF-01 (agregado, erro `indicator.year.invalid`) | `86d9346` |
| RF-01, RN-01, RN-02 (tabela e índice único) | `b664e40` |
| RF-02, RN-04 (comando, handler, caso de uso) | `e2695d6` |
| RN-05, RN-06 (provider do Banco Central) | `b069496` |
| RF-03 (mutation) | `cd026a3` |
| RF-04 | pendente |

## 8. Primeira carga (2026-10-11)

| Indicador | Valores | De | Até |
|---|---|---|---|
| `Dollar` | 10.492 | 28/11/1984 | 09/10/2026 |
| `Cdi` | 10.158 | 06/03/1986 | 08/10/2026 |
| `SelicTarget` | 10.083 | 05/03/1999 | 11/10/2026 |
| `Ipca` | 561 | 01/1980 | 09/2026 |

Total de **31.294** valores. Conferências: todo ano de IPCA anterior a 2026 tem 12 meses; **todo pregão do Ibovespa desde 01/07/1994 tem o dólar do mesmo dia** (nenhum faltando). No dólar do 1º turno de 2018: 3,8699 (05/10), 3,7588 (08/10) e 3,6368 (29/10).

## 9. Próximos passos

1. Query de leitura (RF-04), em conjunto com a de `index_quotes`.
2. Política de nova tentativa no cliente do Banco Central.
3. Decidir o tratamento do dólar anterior ao Real (RN-03), se algum estudo precisar dele.
4. Teste de contrato externo semanal contra a API do SGS, no mesmo workflow do endpoint da B3.
