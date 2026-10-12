# TDR-003 — `ImportHolidaysUseCase` ainda lê por `IApplicationDbContext` e inlina a escrita

- **Data:** 2026-10-11
- **Status:** aceito (dívida conhecida, sem prazo)
- **Commits:** `3676d8b` (caso de uso original), `02805f5` (várias fontes)
- **Quadrante (Fowler):** deliberado + prudente — o padrão correto nasceu depois do código, e a divergência está registrada desde o início

---

## Contexto

O padrão de duas camadas (caso de uso sobre um `CommandHandler`) e a regra "um comando lê antes de escrever pelo repositório, com uma *specification*, nunca por `IApplicationDbContext`" nasceram com a importação da ANP, depois de `importHolidays` (28/06/2026). O caso de uso de feriados ficou como estava.

## Débito assumido

`ImportHolidaysUseCase` injeta `IApplicationDbContext` para ler as chaves `(data, país)` existentes e `IRepository<Holiday>` para escrever, tudo dentro do caso de uso. Não há `ImportHolidaysCommand` nem handler. É o único write do projeto que ainda faz a leitura pelo contexto de leitura.

## Impacto

- Quebra a separação entre os dois contratos (leitura por `IApplicationDbContext`, escrita pelo repositório) justamente no único lugar em que o projeto a viola.
- O caso de uso mistura orquestração (juntar as fontes) com a regra de escrita (dedupe), o que dificulta testar uma sem a outra.
- Carrega **todas** as chaves `(data, país)` do banco a cada importação (cerca de 1.500 linhas hoje); não é um problema de desempenho, mas é desnecessário.

## Alternativa correta (não implementada)

Extrair `ImportHolidaysCommand` e `ImportHolidaysCommandHandler`, que lê as chaves existentes por uma `HolidaysByCountrySpecification` (já existe, usada pela reconstrução da inflação) e escreve pelo repositório; o caso de uso fica só com a junção das fontes e a chamada ao handler. É o mesmo desenho de `ImportIndicatorValuesCommandHandler`.

## Quando resolver

Quando o importador de feriados for tocado de novo (uma terceira fonte, ou a correção de TDR-005). Não bloqueia nada.
