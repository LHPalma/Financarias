# TDR-008 — Códigos antigos que mudaram de classe, e tickers emendados por fusão

- **Data:** 2026-10-11
- **Status:** aberto (dívida conhecida)
- **Commits:** `d14c688` (coluna `legacy_ticker`), `2bfe4ce` (conversão do código antigo pelo ISIN), `fa1d5fc` (SRS)
- **Quadrante (Fowler):** deliberado + prudente — a conversão pela raiz do ISIN acerta a imensa maioria, e a exceção está registrada

---

## Contexto

Antes de 2000 a B3 publicava códigos de três letras e um dígito (`VAL 4`, `ITAU4`). O parser converte para o ticker atual pela **raiz do ISIN** e guarda o original em `legacy_ticker`, de modo que a pesquisa pelo nome antigo continua possível.

## Débito assumido

1. **A conversão pela raiz erra quando a classe mudou de número.** `VAL 4` virou `VALE4` pela raiz, mas a ação preferencial hoje é `VALE5`. Uma tabela de exceções para esses casos não existe (SRS 0007, RN-16).
2. **Tickers que mudaram por fusão não são emendados.** O Itaú muda de `ITAU4` (até 19/05/2009) para `ITUB4` (a partir de 20/05/2009), por causa da fusão com o Unibanco. A série contínua exige emendar os dois códigos, e não é só renomear: houve troca de ações.

## Impacto

- Uma consulta por `VALE5` não encontra a história anterior a quando o número da classe mudou, e uma por `ITUB4` perde o Itaú antes de 2009.
- Para estudos que cobrem a fusão (a eleição de 2002, por exemplo, cai em `ITAU4`), o papel precisa ser tratado à mão.

## Alternativa correta (não implementada)

(a) Uma tabela de **exceções de código** `(legacy_ticker, ticker)` consultada pelo parser antes da conversão pelo ISIN, semeada com os casos conhecidos (`VAL 4`, `VAL 6`…); (b) uma tabela de **sucessão** `(ticker antigo, ticker novo, data de corte, razão de troca)` para as fusões, que a série contínua e a ajustada (TDR-006) consomem. Ambas são pequenas e cabem num *seed* versionado.

## Quando resolver

Quando um relatório precisar de uma série contínua de um papel que passou por troca de classe ou fusão (Itaú, Vale, Bradesco…). É dependência da série ajustada.
