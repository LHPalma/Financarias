# TDR-002 — Mutations de importação e de manutenção são públicas

- **Data:** 2026-10-11
- **Status:** aceito (dívida assumida deliberadamente)
- **Commits:** `841d915` (`importHolidays`), `f12c5a2`, `661cfb4`, `cd026a3`, `d190e08`, `a1a0ae6`, `fc52f0b`
- **Quadrante (Fowler):** deliberado + prudente — o custo é conhecido e está escrito no contrato de cada operação; a correção depende de um slice que ainda não existe (papéis e autorização)

---

## Contexto

Todas as cargas do armazém (`importHolidays`, `importFuelPrices`, `importDailyQuotes`, `importIndexQuotes`, `importIndicatorValues`, `importCorporateEvents`, `importIssuers`) e a reconstrução do índice de inflação (`rebuildInflationLevels`) são mutations GraphQL (ADR-006). A autenticação por JWT existe desde o PR #32, e `[Authorize]` protege `users`, `user`, `me`, `activateUser` e `deactivateUser`. Mas só a **identidade** foi protegida: dados de mercado e importações ficaram abertos, porque não há papel para distinguir quem pode carregar dados.

## Débito assumido

Qualquer chamador, autenticado ou não, dispara uma carga pesada: um ano de COTAHIST, a baixa de uma planilha, centenas de chamadas à B3. Cada operação diz isso no schema ("hoje não exige autenticação: qualquer chamador pode dispará-la").

## Impacto

- **Custo e disponibilidade.** Um chamador anônimo pode ocupar a API e o banco com cargas longas (o limite de execução é de 10 minutos), e consumir a cota das fontes externas.
- **Integridade.** As cargas são idempotentes, então repetir não corrompe; mas `rebuildInflationLevels` e `importHolidays` mudam dados de que outros cálculos dependem.
- Enquanto a API só roda em `localhost`, o risco é teórico. Muda no dia em que ela for exposta.

## Alternativa correta (não implementada)

Introduzir **papéis** (a lacuna aberta nº 1 do projeto) e proteger as mutations de manutenção com uma política, por exemplo `[Authorize(Policy = "DataAdmin")]`, mantendo públicas as consultas de mercado. Uma ADR deve decidir se o papel mora no usuário ou se a permissão é por recurso.

## Quando resolver

**Antes de qualquer exposição fora da máquina de desenvolvimento**, e junto do slice de papéis e autorização. Até lá, a mitigação é operacional: não publicar a API.
