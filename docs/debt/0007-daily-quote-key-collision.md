# TDR-007 — Colisão da chave única de `daily_quotes` (caso `BIOM12F`)

- **Data:** 2026-10-11
- **Status:** aberto (dívida conhecida; efeito raro)
- **Commits:** `bb3f13d` (tabela e índice único), `fa1d5fc` (SRS com a colisão)
- **Quadrante (Fowler):** inadvertido + prudente — a chave `(ticker, data, tipo de mercado)` foi escolhida sobre uma amostra de um dia sem duplicata; a amostra maior mostrou o caso depois

---

## Contexto

A chave natural das cotações é `(ticker, data do pregão, tipo de mercado)` (ADR-007, SRS 0007 RN-02). Numa amostra de um dia (1.847 linhas à vista) não havia duplicata, e acrescentar o `CODBDI` à chave também não gerava.

## Débito assumido

Numa amostra maior, novembro de 2022 (33.339 linhas à vista e fracionárias), apareceram **5 chaves repetidas**, todas do `BIOM12F` no mercado fracionário. Duas espécies diferentes (`BNS ORD MA`, ISIN `BRBIOMN03OR2`, e `ON  REC MA`, ISIN `BRBIOMR12OR4`) negociam sob o mesmo código. O import trata a segunda linha como atualização da primeira (RN-12), então **uma das duas se perde**.

## Impacto

Pequeno e conhecido: poucas linhas por mês, no fracionário, de um papel de baixa liquidez. Não afeta o mercado à vista dos papéis estudados. O `CODBDI` não resolve: ele classifica, não identifica.

## Alternativa correta (não implementada)

Incluir o **ISIN** no índice único `(ticker, data, tipo de mercado, isin)`. Como a chave primária é `id` (ADR-007), é uma migration de índice, sem mexer na identidade das linhas. O custo é um índice maior numa tabela de 7,2 milhões de linhas, e a revisão da *specification* de upsert, que hoje pareia por ticker e data.

## Quando resolver

Se um estudo precisar do fracionário, ou se a contagem de colisões crescer (a importação já devolve `skipped` e as atualizações; um `valuesUpdated` anormal em arquivo novo é o sinal). Hoje não vale o custo do índice.
