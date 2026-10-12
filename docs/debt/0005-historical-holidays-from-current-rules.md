# TDR-005 — Feriados de 1986 a 1990 vêm de regras de hoje e erram dias em que a bolsa operou

- **Data:** 2026-10-11
- **Status:** aceito (dívida assumida deliberadamente)
- **Commits:** `65bc046` (provedor da BrasilAPI), `6158bb3` (a ressalva no contrato GraphQL)
- **Quadrante (Fowler):** deliberado + prudente — o erro é conhecido, medido e de efeito pequeno, e a alternativa exigiria uma fonte que não foi achada

---

## Contexto

A ANBIMA só publica feriados de 2001 em diante, e o índice de inflação (ADR-003) precisa de dias úteis desde 1980. A BrasilAPI cobre 1900–2199, mas **calcula os feriados por regra, aplicando a lei de hoje**. O provedor usa a BrasilAPI de 1980 a 2000.

## Débito assumido

De 1986 a 1990 a regra de hoje marca como feriado dez dias em que a B3 (então as bolsas) **operou**: Corpus Christi, Tiradentes, Nossa Senhora Aparecida e Finados. Os anos 1980 a 1985 e 1991 a 2000 não têm esse problema conhecido, mas também não foram conferidos contra um calendário histórico.

## Impacto

- A contagem de dias úteis desses cinco anos pode errar de um a dois dias por ano.
- No índice de inflação, um dia útil a mais ou a menos numa janela de cerca de 21 desloca o expoente `DU(início, d] / DU(início, fim]`. O efeito é de segunda ordem (a taxa mensal é distribuída por poucos dias a mais ou a menos), mas existe.
- Para o dia de pregão, não importa: ele vem do COTAHIST, que tem as datas reais.

## Alternativa correta (não implementada)

Uma tabela manual de exceções para 1986–1990, a partir de um calendário histórico da bolsa (ou do próprio COTAHIST: os dias com negócio são, por definição, úteis), aplicada por cima da BrasilAPI. O jeito mais barato é derivar, dos pregões já importados, os dias de semana em que houve negócio e removê-los da lista de feriados desse período.

## Quando resolver

Se um estudo depender da precisão diária do deflator entre 1986 e 1990. Para estudos de 1994 em diante, não tem efeito.
