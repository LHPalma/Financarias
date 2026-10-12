# SRS — Precificação da NTN-B Principal (PU, VNA projetado e cenário de compra e venda)

- **Data:** 2026-10-11 (escrito depois da entrega, de 27 a 29/06/2026)
- **Feature:** `Analytics` (`Domain/Analytics`, `Domain/Calendar`, `Application/Analytics`)
- **Status:** **entregue.** Cálculo puro, sem persistência. O VNA e a projeção do IPCA **vêm do chamador**: o projeto não importa o VNA da ANBIMA.
- **Metodologia:** ANBIMA, *Metodologias de Precificação de Títulos Públicos* (quadro de truncamentos, seção 7), e STN, *Metodologia de Cálculo dos Títulos Públicos Federais* (NTN-B).

---

## 1. Introdução

### 1.1 Propósito
Calcular o preço unitário (PU) de uma NTN-B Principal (Tesouro IPCA+ sem cupom) e simular o resultado de comprar numa taxa e vender noutra. É um portado do projeto Java `finance`, mas **não é cópia**: o Java errava a metodologia em quatro pontos (§3), e aqui foram corrigidos.

### 1.2 Escopo
Dentro: PU por dias úteis, PU por datas (liquidação T+2, VNA projetado, dias úteis até o vencimento) e cenário compra/venda. Fora: NTN-B com cupom, outros títulos (LTN, NTN-F, LFT), importação do VNA publicado, curva de juros e mercado secundário.

## 2. Requisitos

### RF-01 — PU por taxa e dias úteis
`NtnbPricing.UnitPrice(vna, yield, businessDays)`. Taxa em **fração** (`0.07` = 7% a.a.).

### RF-02 — PU por datas
`calculateNtnbPrice(input: { vnaBase, yield, inflation, tradeDate, dueDate })` devolve `{ settlementDate, projectedVna, businessDaysToMaturity, unitPrice }`.

### RF-03 — Cenário compra e venda
`simulateNtnbScenario(vna, buyYield, sellYield, businessDays)` calcula os dois PU sobre o mesmo prazo, o lucro bruto (`venda - compra`) e a rentabilidade (`lucro / compra`, arredondada a 4 casas e expressa em %).

## 3. Regras de negócio

### RN-01 — Truncar, não arredondar, em cada etapa
| Etapa | Fórmula | Casas |
|---|---|---|
| Exponencial | `(1 + i)^(du/252)` | truncada em **14** |
| Cotação | `100 / Exponencial` | truncada em **4** |
| PU | `VNA × Cotação / 100` | truncado em **6** |

`Truncation.Truncate` multiplica por 10ⁿ, usa `decimal.Truncate` e divide (`decimal.Round` **não** trunca). O Java arredondava (`HALF_UP`) onde a ANBIMA trunca.

### RN-02 — Ano de 252 dias úteis
O expoente é `du / 252`. A conta de potência passa por `double` (`Math.Pow`), porque o BCL não tem `Pow` para `decimal`, e o resultado volta a `decimal` já truncado.

### RN-03 — Liquidação em T+2
`liquidação = AddBusinessDays(negociação, 2)`, com os feriados do país (`0015`).

### RN-04 — `du` conta de `[liquidação, vencimento)`
Liquidação **inclusive**, vencimento **exclusive**: `Count(liquidação - 1, vencimento - 1)`. O Java incluía o vencimento e errava um dia.

### RN-05 — VNA projetado: pró-rata em dias **corridos**, entre dias 15
`VNA_proj = trunc6(VNA_base × (1 + π)^pr1)`, com `pr1 = trunc14(dias corridos(liquidação − 15 âncora) / dias corridos(15 seguinte − 15 âncora))`. O 15 âncora é o aniversário do IPCA: liquidação a partir do dia 15 usa o 15 do mês corrente; antes, o 15 do mês anterior. Exemplo da STN: liquidação 21/05/2008, `pr1 = 6/31`. **A projeção não usa feriados.** O Java usava dias úteis e âncora no primeiro e último dia do mês, ambos errados.

> Contraste com o índice de inflação diário (`0011`): lá a interpolação **usa** dias úteis, entre dias 15 ajustados, porque segue o *Manual de Curvas da B3*. São duas metodologias oficiais diferentes para dois usos diferentes (o VNA do título público e a curva de mercado).

### RN-06 — Validações com código
`analytics.nominalvalue.invalid` (VNA ≤ 0), `analytics.yield.invalid` (taxa ≤ −100%), `analytics.businessdaycount.invalid` (dias úteis negativos), `analytics.installmentcount.invalid` e `analytics.monthlyrate.invalid` pertencem ao simulador de financiamento (`0003`) e dividem o catálogo `AnalyticsErrors`.

### RN-07 — Sem feriados importados, contagem sem eles
Como em `0015`: o resultado vale para o calendário que estiver no banco, e sem `importHolidays` só os fins de semana são descontados.

## 4. Camadas
```
calculateNtnbPrice → ICalculateNtnbPriceUseCase  (VOs: NominalValue, AnnualYield)
                   → IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult>
                        lê os feriados do intervalo; chama NtnbDatePricing.Calculate
simulateNtnbScenario → ISimulateScenarioUseCase → SimulateScenarioQueryHandler (puro)
```
`CalculateNtnbPriceMapper` (Mapperly) converte o resultado de domínio (`NtnbPrice`) no DTO `NtnbPriceResult`; o cenário é puro e devolve o DTO direto.

## 5. Rastreabilidade
| Commit | Entrega |
|---|---|
| `d389e47`, `9459fd2`, `da9e623` | VOs `AnnualYield`, `NominalValue`, `BusinessDayCount` |
| `dc1a779`, `bb7fd25` | `NtnbPricing` com a metodologia da ANBIMA e o auxiliar `Truncation` |
| `919044b` | Cenário de compra e venda por GraphQL |
| `47ac4a3`, `bb5c5d2`, `b3f1750` | `VnaProjection`, `NtnbDatePricing` e testes |
| `39926c9`, `fdb5512`, `05f0184`, `fab10d7` | Mapperly, caso de uso, query `calculateNtnbPrice` e testes do slice |

## 6. Testes
Domínio: PU contra valores conhecidos, truncamento etapa a etapa, `VnaProjection` com o exemplo da STN (6/31) e as bordas do dia 15, `NtnbDatePricing` ponta a ponta com T+2 sobre feriado. Aplicação: o caso de uso e o cenário. Infraestrutura (Testcontainers): `CalculateNtnbPriceQueryHandler` lendo feriados de um Postgres real.

## 7. Limites conhecidos
- O **VNA base e a inflação projetada são entradas**. Não há importação do VNA da ANBIMA nem série histórica. O TimescaleDB para séries temporais, previsto desde o início para o VNA, continua sem uso.
- Só a NTN-B Principal, só o preço, sem curva.
- A conta de potência usa `double`: a última casa do expoente pode diferir de uma implementação em precisão arbitrária. Os truncamentos seguintes escondem a diferença nos casos testados, mas não há prova geral.
