# SRS — Simulador de Financiamento (Tabela Price)

- **Data:** 2026-07-25
- **Feature:** `Analytics/Financing`
- **Status:** v1 entregue e commitada na `main` (10 commits `feat(financing)`), 326 testes verdes. IOF/CET ficam para o PR seguinte (§10).
- **Fonte externa:** nenhuma (cálculo puro, sem porta de saída)

---

## 1. Introdução

### 1.1 Propósito
Especificar o simulador de financiamento pelo **Sistema Francês de Amortização** (Tabela Price): parcela fixa (PMT), evolução do saldo devedor mês a mês e simulação de quitação antecipada.

É um módulo **apêndice** ao núcleo do produto (carteira consolidada + backoffice), da mesma família do simulador de NTN-B: calculadora pura de domínio, sem I/O, sem gateway, sem persistência. O caso de uso motivador é financiamento de **veículo**, mas a matemática é agnóstica ao bem.

### 1.2 Escopo desta entrega (v1)
- Cálculo da parcela fixa (PMT) dado principal, taxa mensal e número de parcelas.
- Tabela de evolução do saldo devedor (juros × amortização × saldo, parcela a parcela).
- Simulação de quitação antecipada numa parcela arbitrária, com o valor economizado.

**Fora de escopo na v1, planejado para o PR seguinte:**
- **IOF** (fixo + diário) e **CET** (Custo Efetivo Total). Movidos para o PR seguinte porque são a única parte que exige *configuração* (alíquotas em `appsettings`), *datas de calendário* (o IOF diário depende de dias corridos) e *método numérico iterativo* (o CET é uma IRR, sem forma fechada). Ver §10.
- **Datas de vencimento** (`disbursementDate`, carência de 30 dias, `dueDate` por parcela) — entram junto com o IOF, que precisa delas de qualquer forma.

**Fora de escopo indefinidamente:**
- **SAC** e demais sistemas (SACRE, Americano) — no mercado brasileiro, veículo é praticamente só Price; SAC é caso de imobiliário. Ver §11.
- **Consórcio** (mecânica sem juros, não é amortização).
- **Financiamento imobiliário** (SFH/SFI, indexadores IPCA/TR).
- **Tabela FIPE** para sugerir o principal a partir de marca/modelo/ano — seria um slice de integração externa, não de cálculo.

### 1.3 Definições
| Termo | Significado |
|---|---|
| **Sistema Francês / Tabela Price** | Sistema de amortização de **parcela constante**: os juros decrescem e a amortização cresce ao longo do prazo. |
| **PMT** | A parcela fixa, calculada pela fórmula de anuidade constante. |
| **Amortização** | A porção da parcela que abate o principal (`parcela − juros`). |
| **Saldo devedor** | Principal ainda não amortizado após uma parcela. Na Price, **é** o valor presente das parcelas restantes. |
| **Quitação antecipada** | Liquidação do saldo devedor antes do prazo. O valor devido é o **saldo devedor**, não a soma das parcelas restantes. |
| **Resíduo** | Diferença de centavos entre o principal e a soma das amortizações, causada pelo arredondamento da PMT. Absorvido pela última parcela. |
| **IOF / CET** | Imposto sobre Operações Financeiras / Custo Efetivo Total. Fora da v1 (§10). |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura (Clean Architecture)
```
Domain/Analytics
├── NominalValue              (VO já existente — reusado como principal)
├── MonthlyRate               (VO novo)
├── InstallmentCount          (VO novo)
├── Exceptions/{InvalidMonthlyRate, InvalidInstallmentCount, InvalidPayoffPeriod}Exception
└── Financing/
    ├── FrenchAmortization    (calculadora pura, static)
    ├── AmortizationSchedule  (record de saída + PayoffAt)
    ├── InstallmentRow        (record de linha da tabela)
    └── EarlyPayoff           (record de saída da quitação)

Application/Analytics/Financing
├── DTOs/Requests/{SimulateFinancing, SimulateEarlyPayoff}Request
├── DTOs/Results/{FinancingSimulationResult, InstallmentBreakdownResult, EarlyPayoffResult}
├── Mappers/{SimulateFinancing, SimulateEarlyPayoff}Mapper   (Mapperly, nos dois sentidos)
├── Queries/{SimulateFinancing, SimulateEarlyPayoff}{Query, QueryHandler}
└── UseCases/{ISimulateFinancing, ISimulateEarlyPayoff}UseCase (+ impls)

Api/GraphQL/Query.cs           (resolvers simulateFinancing, simulateEarlyPayoff)
```

Os mappers mapeiam **nos dois sentidos** — request cru → query com VOs, e record de domínio → DTO. O caminho de volta existe (diferente do `CalculateNtnbPriceMapper`, onde o handler monta o resultado à mão) porque a tabela tem dezenas de linhas de campos idênticos: a projeção manual seria um `Select` que só renomeia a coleção.

**Um mapper por operação**, não um `FinancingMapper` compartilhado: duplica três conversores de uma linha cada, mas mantém o mapeamento de cada operação independente — o mesmo motivo pelo qual há um use case por operação.

**A quitação não é uma segunda calculadora.** `PayoffAt` é um método sobre a `AmortizationSchedule` que a `FrenchAmortization` já produz; o handler monta a tabela como o de simulação e lê a linha `k`.

**Por que os VOs ficam na raiz de `Analytics` e não em `Financing/`:** mesma razão pela qual `AnnualYield`/`BusinessDayCount` estão na raiz e não em `Ntnb/` — a subpasta guarda os tipos da metodologia, os VOs são o vocabulário da área. As exceções seguem o prefixo de código já usado (`analytics.*`).

**Por que não há entidade, repositório ou porta:** é uma calculadora, não um agregado. Nada é persistido, nada é buscado. Diferente de todos os slices de market-data até aqui, este não tem `Gateway`.

### 2.2 Fluxo (Estilo A — resolver → UseCase → QueryHandler → resultado)
`GraphQL resolver` → `UseCase` (mapper valida os crus em VOs, lançando exceção de domínio) → `QueryHandler` (chama a calculadora, monta o DTO) → resposta. Sem I/O em nenhum ponto — o `QueryHandler` aqui não injeta `IApplicationDbContext` nem porta alguma.

---

## 3. Requisitos funcionais

### RF-01 — Calcular a parcela fixa (PMT)
Dado principal `P`, taxa mensal `i` (fracionária) e número de parcelas `n`:

```
i > 0:   PMT = P · i · (1+i)^n / ((1+i)^n − 1)
i = 0:   PMT = P / n
```

O resultado é **arredondado a 2 casas** e é esse valor arredondado que alimenta a tabela (RN-01).

### RF-02 — Gerar a tabela de evolução do saldo devedor
Para cada parcela `k = 1..n`:
- `juros(k) = arredonda(saldo(k−1) × i, 2)`
- `amortização(k) = PMT − juros(k)`
- `saldo(k) = saldo(k−1) − amortização(k)`

Na **última parcela**, a amortização é o saldo devedor remanescente inteiro e a parcela vira `juros(n) + saldo(n−1)` — absorvendo o resíduo e zerando o saldo (RN-01).

Cada linha carrega também `accumulatedInterest` — a soma dos juros das parcelas `1..k`. É o único acumulado que a linha não permite deduzir sozinha (o amortizado acumulado é `principal − saldo`, e o pago acumulado é `parcela × k`), e é o espelho retrospectivo do `interestSaved` do RF-03: na parcela `k` o cliente vê quanto de juro já pagou e quanto ainda evita quitando ali.

A tabela expõe também os agregados `installment` (a PMT), `totalPaid` e `totalInterest`.

### RF-03 — Simular quitação antecipada
Dada uma parcela `k` em `1..n`, retorna:
- `outstandingBalance` — o saldo devedor após a parcela `k` (é o valor da quitação);
- `installmentsRemaining` = `n − k`;
- `interestPaid` = o `accumulatedInterest` da linha `k`;
- `interestSaved` = `totalInterest − interestPaid`.

**Não é matemática nova.** Na Price o saldo devedor já é o valor presente das parcelas restantes, ou seja, já está com os juros futuros descontados pro rata. A query existe para entregar o número que o usuário quer ver (*quanto eu economizo quitando agora*), não porque haja um segundo cálculo (RN-04).

`interestPaid` e `interestSaved` são os dois lados da mesma moeda e somam `totalInterest`. O par responde "vale a pena quitar?" numa olhada — e expõe a assimetria da Price: em 30.000 a 1,5% em 10 parcelas, quitar na 5ª mostra 1.823,19 já pagos contra 707,06 economizados, ou seja **72% do juro consumido em 50% do prazo**.

**A economia é calculada por `totalInterest − interestPaid`** (O(1)), não somando as parcelas restantes. Os dois caminhos são algebricamente idênticos — a soma das parcelas `k+1..n` é *(juros restantes)* + *(amortizações restantes)*, e as amortizações restantes somam exatamente o saldo devedor. O teste percorre o **outro** caminho, provando a identidade em vez de repetir a implementação; como todos os valores já são decimais de 2 casas somados exatamente, a igualdade é exata e dispensa tolerância.

**Validação do período** não cabe num VO: o limite superior é `n`, conhecido só em runtime. Fica em `AmortizationSchedule.PayoffAt`, que lança `InvalidPayoffPeriodException`. `k = n` é válido e degenerado (saldo 0, economia 0). `k = 0` fica de fora — matematicamente seria "quitar antes da 1ª parcela", o que descreve alguém que pegou o empréstimo e devolveu no mesmo dia.

### RF-04 — Vocabulário de entrada validado no domínio
`principal`, `monthlyRate` e `installments` chegam crus na fronteira e são convertidos em `NominalValue`, `MonthlyRate` e `InstallmentCount` no mapper. Entrada inválida lança exceção de domínio antes do handler, e o `DomainErrorFilter` a converte em erro GraphQL com `extensions.code`.

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | **A PMT arredondada a 2 casas é dado de entrada da tabela, não apresentação.** É o que o banco cobra de fato (R$ 881,25, não R$ 881,2499882…), e é o que faz nascer o resíduo que a última parcela absorve. Isso **diverge conscientemente** da regra do câmbio (RN-02 do SRS de FX, "arredondar só na borda"): lá o arredondamento não realimenta o cálculo, aqui realimenta. |
| **RN-02** | **Juros de cada período também são arredondados a 2 casas** antes de virar amortização — pelo mesmo motivo: é um valor cobrado, não um intermediário. |
| **RN-03** | **Taxa zero é entrada válida**, com ramo próprio `PMT = P/n`. A fórmula de anuidade divide por `(1+i)^n − 1`, que é zero quando `i = 0`; e "0% de juros" existe no mundo real (promoção de concessionária). Taxa **negativa** é inválida. |
| **RN-04** | **Quitação antecipada = saldo devedor**, nunca a soma das parcelas restantes (que superestimaria o custo, cobrando juros ainda não incorridos). |
| **RN-05** | **Potenciação de expoente inteiro é feita em `decimal` por multiplicação repetida**, não via `Math.Pow`. Diferente do `NtnbPricing`, que precisa de `double` porque o expoente é fracionário (`du/252`), aqui `n` é inteiro e o RNF de precisão monetária (RNF-02) pode ser cumprido integralmente. |
| **RN-06** | A soma das amortizações é **exatamente** o principal, e o saldo após a última parcela é **exatamente** zero. São invariantes verificáveis, não aproximações. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Testabilidade):** toda a lógica numa função pura, testável sem I/O nem mocks — mesmo padrão de `NtnbPricingTests` e `CurrencyConverterTests`. O `QueryHandler` não tem dependência para substituir.
- **RNF-02 (Precisão monetária):** pipeline inteiro em `decimal`, nunca `double` (ver RN-05 para como isso é sustentado). O CET, no PR seguinte, precisará afrouxar isso ou definir tolerância explícita — ver §10.
- **RNF-03 (Sem configuração):** a v1 não lê nada de `appsettings`. É o primeiro slice sem nenhuma chave de configuração — e a razão principal para o IOF ficar de fora (§10).
- **RNF-04 (Consistência de fronteira):** a tabela é exposta como `IReadOnlyList<InstallmentBreakdownResult>` no GraphQL, seguindo o que o slice de câmbio estabeleceu (RNF-05 do SRS de FX).
- **RNF-05 (Limite de prazo):** `InstallmentCount` aceita `1..600`. O teto existe porque a potenciação de RN-05 é um laço — 600 parcelas (50 anos) cobre qualquer produto real com folga e evita que uma entrada absurda vire trabalho inútil.

---

## 6. Interface — GraphQL

```graphql
type Query {
  simulateFinancing(input: SimulateFinancingInput!): FinancingSimulationResult!
  simulateEarlyPayoff(input: SimulateEarlyPayoffInput!): EarlyPayoffResult!
}

input SimulateFinancingInput {
  principal: Decimal!
  monthlyRate: Decimal!      # fracionária: 0.015 = 1,5% a.m.
  installments: Int!
}

input SimulateEarlyPayoffInput {
  principal: Decimal!
  monthlyRate: Decimal!
  installments: Int!
  atInstallment: Int!
}

type EarlyPayoffResult {
  period: Int!
  outstandingBalance: Decimal!
  installmentsRemaining: Int!
  interestPaid: Decimal!
  interestSaved: Decimal!
}

type FinancingSimulationResult {
  installment: Decimal!       # a PMT
  totalPaid: Decimal!
  totalInterest: Decimal!
  schedule: [InstallmentBreakdownResult!]!
}

type InstallmentBreakdownResult {
  period: Int!
  installment: Decimal!
  interest: Decimal!
  accumulatedInterest: Decimal!
  amortization: Decimal!
  outstandingBalance: Decimal!
}

type EarlyPayoffResult {
  atInstallment: Int!
  outstandingBalance: Decimal!
  installmentsRemaining: Int!
  interestSaved: Decimal!
}
```

**Nome `simulateFinancing`, não `simulateVehicleFinancing`:** a matemática não sabe que o bem é um carro. O imobiliário está fora do escopo por *indexador* (IPCA/TR) e por *sistema* (SAC), não por incompatibilidade com a Price. Renomear API pública depois é caro; nascer neutro é grátis.

**`totalIof` e `cet` entram no `FinancingSimulationResult` no PR seguinte** — adicionar campo a um `type` é mudança aditiva em GraphQL, não quebra cliente.

---

## 7. Casos de referência (oráculo dos testes)

Os testes de domínio não podem se auto-validar. Estes valores foram calculados de forma independente e servem de golden values:

| Principal | Taxa a.m. | Parcelas | PMT crua | PMT (2 casas) |
|---|---|---|---|---|
| 30.000,00 | 1,50% | 48 | 881,2499882449 | **881,25** |
| 50.000,00 | 1,99% | 60 | 1.434,9177724568 | **1.434,92** |
| 12.000,00 | 2,49% | 24 | 670,2128307024 | **670,21** |
| 10.000,00 | 0,00% | 10 | 1.000,0000000000 | **1.000,00** (ramo `P/n`, RN-03) |

**Tabela do primeiro caso** (30.000 / 1,5% / 48), demonstrando o resíduo de RN-01:

| k | parcela | juros | amortização | saldo |
|---|---|---|---|---|
| 1 | 881,25 | 450,00 | 431,25 | 29.568,75 |
| 2 | 881,25 | 443,53 | 437,72 | 29.131,03 |
| … | … | … | … | … |
| 47 | 881,25 | 25,85 | 855,40 | 868,26 |
| 48 | **881,28** | 13,02 | 868,26 | **0,00** |

Total pago 42.300,03 · total de juros 12.300,03. A última parcela é 3 centavos maior que as outras — esse é exatamente o comportamento que RN-01 escolhe ter, e um teste deve travá-lo.

---

## 8. Rastreabilidade (requisito → commit)

| Commit | Entrega |
|---|---|
| `17e5a6e` | `MonthlyRate` + teste — RF-04, RN-03 (taxa zero válida via `IsZero`) |
| `f53aefa` | `InstallmentCount` + teste — RF-04, RNF-05 |
| `07612d9` | `FrenchAmortization` + records + teste — RF-01, RF-02, RN-01, RN-02, RN-05, RN-06 |
| `7122f25` | query + handler + mapper (dois sentidos) + testes — RF-04 |
| `0708a79` | use case + teste |
| `bc25dd4` | wiring DI + GraphQL `simulateFinancing` + funcional — §6 |
| `a834499` | `accumulatedInterest` por parcela (domínio → DTO → GraphQL) + testes — RF-02 |
| `81b6d29` | `EarlyPayoff` + `AmortizationSchedule.PayoffAt` + `InvalidPayoffPeriodException` + testes — RF-03, RN-04 |
| `5dd9865` | query + handler + mapper + use case da quitação + testes |
| `75811fb` | wiring DI + GraphQL `simulateEarlyPayoff` + funcional — §6 |

---

## 9. Cobertura de testes (326 no total, 0 falhas)

| Nível | Cobre |
|---|---|
| `MonthlyRateTests` | fração e percentual; zero é válido (RN-03); negativa lança; igualdade por valor; `ToString` invariante. |
| `InstallmentCountTests` | válidos; zero/negativo/acima de 600 lançam (RNF-05); igualdade por valor. |
| `FrenchAmortizationTests` | PMT bate com os golden values de §7; ramo taxa-zero; resíduo na última parcela; **invariantes de RN-06** (soma das amortizações = principal, saldo final = 0); acumulado de juros fecha no total; monotonicidade (mais parcelas ⇒ parcela menor e juros totais maiores); estouro de `decimal` vira exceção de domínio. |
| `AmortizationScheduleTests` | quitar na parcela `k` devolve o saldo da linha `k`; **a economia pelo caminho independente** (parcelas restantes − saldo) bate com a implementação em 4 períodos; `interestPaid + interestSaved = totalInterest`; quitar em `n` economiza 0; quitar antes economiza mais que depois; parcela fora de `1..n` lança. |
| Handlers | mapeiam domínio → DTO sem perder linha nem agregado; a exceção de período fora do prazo atravessa o handler. |
| Mappers | crus → VOs; cada invariante violada lança sua exceção; `EarlyPayoff` → DTO sem perder campo. |
| Use cases | delegação com a query certa + validação (entrada inválida lança antes do handler, que nunca é chamado). |
| Funcional (GraphQL) | `simulateFinancing` devolve tabela completa; `simulateEarlyPayoff` devolve saldo e economia; entrada inválida vira erro com `extensions.code`. **Sem stub** — o pipeline real roda ponta a ponta, possível porque nenhuma das duas queries sai do processo. |

---

## 10. PR seguinte — IOF e CET

Registrado aqui para não se perder, e para deixar explícito **por que** não está na v1.

- **Datas:** `disbursementDate` (default hoje) + carência de 30 dias; `dueDate(k) = disbursementDate + graceDays + (k−1) meses`. **Sem rolagem para dia útil** na primeira versão — usar `BusinessDayCalendar` + feriados do banco puxaria I/O para dentro de uma calculadora que hoje não tem nenhum, e a diferença é irrelevante para a simulação.
- **IOF:** componente fixo (0,38% sobre o principal) + componente diário sobre o saldo, com teto de dias. As alíquotas mudam por ato normativo, então vêm de `Analytics:Financing:Iof` — **mas não entram no `Domain`**. A calculadora recebe um record `IofRates` como argumento; quem lê `IOptions<>` e monta o record é o `QueryHandler`. O `Domain` continua sem conhecer `Microsoft.Extensions.*`.
- **Options validadas no boot:** hoje **não existe** esse padrão no repo — `AddIntegrations` só lê `BaseUrl` da configuração, sem `ValidateOnStart`. Introduzir `AddOptions<IofOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` deve ser **commit próprio**, antes do IOF, não embutido nele.
- **CET:** é uma IRR sobre o fluxo real (valor liberado × parcelas pagas, com IOF). Não tem forma fechada — exige bisseção ou Newton–Raphson com tolerância, teto de iterações e comportamento definido na não-convergência (exceção de domínio própria). É o ponto onde o RNF-02 ("nunca `double`") precisa ser reescrito para "`decimal` com tolerância declarada".

---

## 11. Itens em aberto

- **SAC como segundo sistema.** Se entrar, o par de nomes já está preparado: `FrenchAmortization` (parcela constante) / `ConstantAmortization` (amortização constante), com a interface comum extraída só quando o segundo existir — não antes. Ainda não decidido se entra no escopo do TCC.
- **Monetização.** Diferente do módulo de combustível (já definido como premium), ainda não está decidido se o simulador é gratuito (ferramenta de aquisição) ou parte da camada premium.
- **Reagrupar `Application/Analytics` por feature.** Hoje a pasta é flat e contém só NTN-B; com Financing entrando, vira mistura. O reagrupamento (`Analytics/Ntnb/…` + `Analytics/Financing/…`) é mecânico e deve ser um `refactor` próprio, antes ou depois desta feature.
- **VO `Rate` unificado.** Com `MonthlyRate` nascendo ao lado de `AnnualYield`, este é o **segundo** caso de taxa no projeto. A decisão de unificar num VO único com a frequência como dado segue **adiada** — dois casos é sinal, não gatilho. O terceiro (CDI diária) força a conversa.
