# SRS — Feriados e contagem de dias úteis

- **Data:** 2026-10-11 (escrito depois da entrega, de 27 a 28/06/2026, e revisado com a segunda fonte de feriados de 2026-10-11)
- **Feature:** `Holidays` + `Domain/Calendar`
- **Status:** **entregue.** Base de tudo que depende de dia útil: NTN-B (`0016`), índice de inflação (`0011`) e a query `businessDayCount`.
- **Fontes externas:** planilha de feriados nacionais da ANBIMA (2001 em diante) e BrasilAPI (1980 a 2000).

---

## 1. Introdução

### 1.1 Propósito
Saber se um dia é útil e quantos dias úteis há entre duas datas, para o Brasil. O calendário é dado (feriados importados e persistidos) e a regra é pura (`BusinessDayCalendar`, no domínio).

### 1.2 Escopo
Dentro: agregado `Holiday`, importação a partir de duas fontes, contagem de dias úteis e soma de dias úteis. Fora: feriados estaduais e municipais, e feriados de outros países (o `CountryCode` existe, mas só `BR` tem fonte).

## 2. Regras

### RN-01 — Dia útil
Segunda a sexta, que não seja feriado do país. Um feriado em fim de semana não muda nada.

### RN-02 — `Count(start, end)` é `(start, end]`
O dia inicial **fica de fora** e o final **entra**. É a convenção que a ANBIMA usa para `du`. Se `start >= end` o resultado é zero. `BusinessDayCount` é um VO que recusa negativo (`analytics.businessdaycount.invalid`).

### RN-03 — `AddBusinessDays(from, n)`
Anda `n` dias úteis para a frente a partir de `from` (exclusive). É o que liquida T+2 na NTN-B e o que acha o "15 ajustado" do índice de inflação.

### RN-04 — Dois países, uma chave
A chave natural é `(data, país)`, e a importação nunca duplica. `Holiday.Create` recusa nome vazio (`holiday.name.required`).

### RN-05 — Duas fontes, a primeira vence
`ImportHolidaysUseCase` recebe todas as `IHolidayProvider` registradas, junta o que cada uma traz e, na mesma data e país, **fica com a primeira** (a ANBIMA é registrada antes). Depois descarta o que já existe no banco e grava só o novo.

| Fonte | Período | Como obtém | Observações |
|---|---|---|---|
| ANBIMA | 2001 em diante | planilha `.xls` (ExcelDataReader), via Refit | cabeçalho e notas de rodapé são ignorados |
| BrasilAPI | 1980 a 2000 | uma chamada por ano | só `type = national`; **ignora Páscoa**; mantém feriado em fim de semana; falha de um ano é registrada e o ano é pulado, sem derrubar os demais (cancelamento não é engolido) |

### RN-06 — A BrasilAPI aplica a lei de hoje ao passado
Ela calcula os feriados por regra. De 1986 a 1990 isso marca como feriado dez dias em que a bolsa **operou** (Corpus Christi, Tiradentes, Aparecida e Finados). O efeito sobre a contagem de dias úteis é de um ou dois dias por ano nesses cinco anos. Está registrado no contrato GraphQL de `importHolidays` e no TDR-005.

### RN-07 — Sem feriado importado, só fim de semana
`businessDayCount` e a NTN-B usam os feriados que estiverem no banco. Sem `importHolidays`, a contagem desconta só sábados e domingos, **sem aviso**.

## 3. Superfície GraphQL
- `businessDayCount(start: Date!, end: Date!, countryCode: CountryCode!): Int!` — sem autenticação.
- `importHolidays: HolidayImportResult!` (`totalFetched`, `totalSaved`) — manutenção, sem autenticação hoje.

## 4. Camadas
`businessDayCount` → `ICountBusinessDaysUseCase` → `IQueryHandler<FindHolidaysInRangeQuery, IReadOnlySet<DateOnly>>` (lê só o intervalo pedido) → `BusinessDayCalendar.Count`. A importação **ainda não** é um `CommandHandler`: o caso de uso lê por `IApplicationDbContext` e escreve pelo repositório (TDR-003).

## 5. Carga atual
1980 a 2099 no banco (120 anos; 2000 tem 11 feriados, os demais 12 ou 13). A planilha da ANBIMA vai até 2099.

## 6. Rastreabilidade
| Commit | Entrega |
|---|---|
| `2b5e317`, `c2d47b0` | `BusinessDayCalendar` e sua mudança para `Domain/Calendar` |
| `21be227`, `1516984`, `902e63b` | Agregado `Holiday`, persistência e testes com Testcontainers |
| `bae8ce2`, `c1cbf1a`, `0d71a75` | Consulta do intervalo, caso de uso e query `businessDayCount` |
| `2bd9aff`, `7af58dd`, `cc79c31`, `a06e686` | Provedor da ANBIMA, cliente Refit, teste da planilha e de contrato |
| `3676d8b`, `841d915`, `d94be4c`, `b1a5cec` | Caso de uso de importação, mutation e testes de dedupe |
| `02805f5`, `65bc046`, `6158bb3` | Importação com várias fontes, BrasilAPI e a documentação da ressalva |

## 7. Testes
Domínio: contagem com e sem feriados, dia inicial excluído, fim de semana, `start >= end`, soma de dias úteis. Aplicação: intervalo, dedupe e precedência entre fontes. Infraestrutura (Testcontainers): persistência, consulta por intervalo, importação com duas fontes. Integrações: leitura da planilha com linhas em branco e rodapé, provedor da BrasilAPI (Páscoa, tipo, falha de um ano), contrato real da ANBIMA.

## 8. Limites conhecidos
RN-06 e RN-07, TDR-003 e a ausência de feriados regionais.
