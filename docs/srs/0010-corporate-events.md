# SRS — Eventos corporativos (proventos, desdobramentos, grupamentos e bonificações)

- **Data:** 2026-10-11
- **Feature:** `MarketData/CorporateEvents` + `Integrations/MarketData/B3/CorporateEvents`
- **Status:** **especificado; implementação em curso.** Os requisitos entregues são marcados em §8.
- **Fonte externa:** dois endpoints do proxy de empresas listadas do site da B3 (`sistemaswebb3-listados.b3.com.br/listedCompaniesProxy/CompanyCall/`), sem chave e **sem contrato publicado** (mesmo caso do Ibovespa, `0008`).
- **Base empírica:** os dois endpoints foram comparados em **2026-10-11** com a Petrobras e com cinco empresas que saíram da bolsa, e os fatores de desdobramento e grupamento foram conferidos contra o preço real em `daily_quotes`.

---

## 1. Introdução

### 1.1 Propósito
Guardar o que acontece com uma ação além do preço: o que a empresa **paga** (dividendo, JCP) e o que **muda a quantidade ou a cotação** das ações (desdobramento, grupamento, bonificação). Sem isso o preço do COTAHIST (`0007`, RN-08) não tem ajuste: um desdobramento vira um "crash" falso, e a data ex de um provento distorce o dia de um evento (o caso da PETR4 em 12/08/2022).

É a base da **série de preço ajustada** e do **retorno total**, que são o próximo slice e o que os relatórios de evento de fato consomem.

### 1.2 Escopo
- Proventos em dinheiro: `DIVIDENDO`, `JRS CAP PROPRIO` (JCP) e `RENDIMENTO`.
- Eventos sobre a quantidade de ações: `DESDOBRAMENTO`, `GRUPAMENTO` e `BONIFICACAO`.
- Importação por emissor, e de todos os emissores, disparada por mutation.

**Fora de escopo (hoje):**
- **Subscrições.** Raras e difíceis de ajustar; o endpoint as traz e ficam para depois.
- **A série ajustada e o retorno total.** Dependem deste slice e têm SRS próprio.
- Leitura por query GraphQL.
- Dados cadastrais da empresa (código CVM, segmento, total de ações, escriturador), que o endpoint 2 também devolve.

## 2. As duas fontes

| | Endpoint 1: `GetListedCashDividends` | Endpoint 2: `GetListedSupplementCompany` |
|---|---|---|
| Pedido | `{"language":"pt-br","pageNumber":1,"pageSize":100,"tradingName":"PETROBRAS"}` em base64 no caminho | `{"issuingCompany":"PETR","language":"pt-br"}` em base64 no caminho |
| Chave de busca | **nome de pregão** (`tradingName`) | **código do emissor** (4 letras) |
| Devolve | proventos em dinheiro, paginado | um array de um objeto: dados da empresa, `cashDividends`, `stockDividends`, `subscriptions` |
| Profundidade dos proventos | **1996 a 2026** (343 registros na Petrobras) | só **2025 e 2026** (24 itens, 10 distintos, todos também no endpoint 1) |
| Eventos sobre ações | não | **sim**, desde 1994 (a bonificação da Petrobras é de 25/03/1994) |
| ISIN | **não** (só a espécie, `ON` ou `PN`) | sim, em cada item |
| Data de pagamento | não | sim (`paymentDate`) |

**Consequência:** os proventos vêm do endpoint 1 (histórico completo), e os eventos sobre as ações vêm do endpoint 2 (única fonte). A data de pagamento só existe para 2025 e 2026.

### 2.1 Formato
Números em **pt-BR como texto** (`"0,47156696"`, `"1.000,00"`), datas em `dd/MM/yyyy`, uma data ISO solta (`lastDateTimePriorEx`), **datas sentinela** `01/01/1900` e `31/12/9999` quando o dado não existe, e `tradingName` com espaços à direita.

### 2.2 Cobertura de empresas que saíram da bolsa
O cadastro `GetInitialCompanies` lista **3.541 empresas, todas com status `A`**: Aracruz, Sadia, Acesita, Telesp e Brasil Telecom não estão nele. Os endpoints, porém, respondem por algumas delas se o nome ou o código estiver certo, e de forma **irregular**:

| Emissor | Endpoint 1 (nome de pregão) | Endpoint 2 (código) |
|---|---|---|
| `ARCZ` Aracruz | 99 proventos (`ARACRUZ`) | vazio |
| `SDIA` Sadia | 50 proventos com `SADIA S.A.`; **0 com `SADIA S/A`** | vazio |
| `BRTO` Brasil Telecom | 27 proventos (`BRASIL TELEC`) | sem corpo JSON |
| `ACES` Acesita | 0 | 0 proventos e 7 eventos sobre ações |
| `TLPP` Telesp | 0 | sem corpo JSON |

O nome de pregão mudou ao longo do tempo no COTAHIST (`SADIA S.A.` até 19/12/2001, `SADIA S/A` depois), e **só uma das formas** responde. Por isso o importador tenta **todos os nomes distintos** de cada emissor e une o resultado.

## 3. O que foi validado contra o preço real

| Evento | Fator da B3 | Preço não ajustado | Leitura |
|---|---|---|---|
| Desdobramento, PETR, 25/04/2008 | `100` | PETR4 84,30 → 42,59 (×0,505); PETR3 103,00 → 51,85 (×0,503) | `100` é **+100% de ações** (2 por 1) |
| Grupamento, PETR, 21/06/2000 | `0,01` | por ação: R$ 0,515 → R$ 51,51 (×100) | as ações viram **×0,01** (100 por 1) |
| Bonificação, PETR, 25/03/1994 | `33,33` | não conferível | anterior ao primeiro pregão em reais (04/07/1994) |

O grupamento de 21/06/2000 coincide com a troca do **lote de cotação**: o `quote_factor` do COTAHIST foi de 1000 para 1 em 23/06/2000. O preço bruto caiu 10 vezes (×0,1) porque somou um grupamento de 100 por 1 (preço ×100) com a mudança de lote (preço ÷1000). **Ajustar preço exige combinar os eventos com o `quote_factor`**, nunca um só.

A interpretação do fator por tipo (percentual de aumento para desdobramento e bonificação, multiplicador para grupamento) foi conferida em **um só emissor**. Ela é guardada como publicada (RN-09), e a validação em escala fica para a série ajustada.

### 3.1 O tipo `RENDIMENTO`
Na Petrobras são 85 de 343 registros, de 2000 a 2026, com valor médio de R$ 0,024 por ação contra R$ 0,575 do dividendo ou JCP. **Os 85 caem na mesma data-com e espécie de um `DIVIDENDO` ou JCP**, e a razão para o provento é a mesma de uma data para outra (4,07% e 4,97% em 25/04/2024 e em 02/05/2024). A leitura mais provável, **inferida e não confirmada em documento**, é a **correção pela Selic** de dividendo pago em parcelas. Dividendo complementar aparece como `DIVIDENDO` comum, não como `RENDIMENTO`.

### 3.2 Linhas repetidas não são duplicatas
Um provento pago em duas parcelas iguais aparece **duas vezes** no endpoint 1, com a mesma data-com, o mesmo valor e o mesmo tipo (por exemplo, dois `DIVIDENDO` de 0,5502 em 25/04/2024). O endpoint 1 não traz a data de pagamento que distinguiria as parcelas.

### 3.3 Valores antigos por lote
Em 1996 o provento vem como `5,1471` com `ratio` de 1000, ou seja, por **lote de mil ações**, o mesmo problema do `FATCOT` no COTAHIST (`0007`, RN-08).

## 4. Modelo

`CorporateEvent` é raiz de agregado, uma linha por registro publicado pela B3.

| Campo | Notas |
|---|---|
| `IssuerCode` | código do emissor, 4 letras (`PETR`) |
| `Isin` | nulo quando vem do endpoint 1 |
| `ShareClass` | espécie como publicada (`ON`, `PN`…) |
| `Kind` | `CashDividend`, `InterestOnEquity`, `Yield`, `StockSplit`, `ReverseSplit`, `BonusShares` |
| `CumDate` | `lastDatePriorEx`: **último dia com direito** (data-com) |
| `ApprovalDate`, `PaymentDate` | nulos quando ausentes ou sentinela |
| `Amount` e `PerShares` | valor e lote como publicados; valor por ação = `Amount ÷ PerShares` |
| `Factor` | fator dos eventos sobre ações, como publicado |
| `Label` | rótulo original da B3 |

## 5. Regras de negócio

- **RN-01 — Fontes.** Proventos em dinheiro pelo endpoint 1; desdobramento, grupamento e bonificação pelo endpoint 2.
- **RN-02 — Sem chave natural.** Parcelas idênticas coexistem (§3.2), então a tabela **não tem índice único** sobre o evento. A importação de um emissor **substitui o conjunto**: lê o que existe, compara com o que a B3 publica, e só troca se for diferente. Reimportar o mesmo conjunto não altera nada.
- **RN-03 — Valor como publicado.** Guardam-se `Amount` e `PerShares` crus. O valor por ação é derivado.
- **RN-04 — Tipo.** `DIVIDENDO` → `CashDividend`; `JRS CAP PROPRIO` → `InterestOnEquity`; `RENDIMENTO` → `Yield`; `DESDOBRAMENTO` → `StockSplit`; `GRUPAMENTO` → `ReverseSplit`; `BONIFICACAO` → `BonusShares`. Um rótulo desconhecido é **pulado, contado e logado**, não derruba a importação.
- **RN-05 — Sentinelas.** `01/01/1900` e `31/12/9999` viram nulo.
- **RN-06 — Data-com.** É o último dia com direito. O preço cai no pregão seguinte, a data ex.
- **RN-07 — Enumeração.** Os emissores vêm de `daily_quotes`: a raiz do ISIN (posições 3 a 6) é o código do emissor, e todos os `company_name` distintos do emissor são os nomes de pregão a tentar. O cadastro da B3 não é usado, porque só tem empresas ativas (§2.2).
- **RN-08 — Cobertura irregular.** Para empresas que saíram da bolsa a B3 pode não ter eventos. É limitação conhecida da fonte, e a série ajustada precisa de uma **auditoria de saltos** (queda ou alta de preço sem evento correspondente) para sinalizar o que faltou.
- **RN-09 — Fator cru.** `Factor` é guardado como publicado. A leitura por tipo (§3) só é aplicada na série ajustada.
- **RN-10 — JCP bruto.** A B3 publica o valor bruto; o imposto na fonte é tratado na leitura.
- **RN-11 — `Yield` fora do ajuste.** O `RENDIMENTO` é guardado, mas não entra no ajuste de preço: o preço cai pelo provento declarado, e a correção é paga depois, como juros.

## 6. Superfície GraphQL (planejada)

`importCorporateEvents(input: { issuer: "PETR" })` importa um emissor; sem `issuer`, percorre todos. É uma operação pesada (milhares de chamadas à B3) e hoje não exigiria autenticação, como as demais importações.

## 7. Riscos

- **Endpoint sem contrato**, com teste de contrato semanal como mitigação.
- **Volume.** A amostra de 4 datas já tem 1.641 raízes de ISIN; a história inteira tem mais. Cada emissor custa uma chamada ao endpoint 2 e uma ou mais páginas por nome ao endpoint 1.
- **Cobertura de quem saiu da bolsa** (RN-08).
- **Semântica do fator** conferida em um emissor (RN-09).
- **Classes preferenciais.** O endpoint 1 traz só `PN`, mesmo para empresas com mais de uma classe (PNA, PNB); o ISIN do endpoint 2 as distingue, o 1 não.

## 8. Rastreabilidade

| Requisito / regra | Commit |
|---|---|
| (a preencher conforme os commits saírem) | |

## 9. Próximos passos

1. Domínio, persistência, importação e mutation, em commits separados.
2. Carga de todos os emissores, com relatório de cobertura (emissor, nomes tentados, eventos achados).
3. Série de preço ajustada, validada pela auditoria de saltos, e retorno total.
