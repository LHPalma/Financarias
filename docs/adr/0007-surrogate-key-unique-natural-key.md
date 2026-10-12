# ADR-007 — Chave primária substituta (`id`) e índice único na chave natural

- **Data:** 2026-10-11
- **Status:** aceita
- **Commits:** vale para todas as tabelas do armazém; os exemplos são `bb3f13d` (`daily_quotes`), `85c9361` (`index_quotes`), `b664e40` (`indicator_values`), `7dc92eb` (`corporate_events`), `4a0bc30` (`inflation_levels`) e `c13f2df` (`issuers`)
- **Relacionada:** `docs/srs/0007-b3-cotahist.md` (RN-02), `docs/srs/0008-ibovespa-index.md`, `docs/debt/0007-daily-quote-key-collision.md`

---

## Contexto

Quase toda tabela de dados de mercado tem uma chave natural composta: `(ticker, data, tipo de mercado)` nas cotações, `(índice, data)` no Ibovespa, `(indicador, data)` nos indicadores, `(dia)` no índice de inflação, `(código do emissor)` no cadastro. Era preciso decidir se essa chave composta seria a chave primária.

## Decisão

**Toda entidade herda de `BaseEntity<long>` (chave `id` gerada pelo banco) e a chave natural vira um índice único.**

- A chave primária é `pk_<tabela>` sobre `id`.
- A unicidade da chave natural é um índice único nomeado (`ix_daily_quotes_ticker_trading_date_market_type`, `ix_issuers_issuer_code`, …).
- Invariantes que importam em repouso viram `CHECK` (por exemplo, preços positivos nas cotações), para valerem também para carga em massa.

A orientação veio do usuário ao desenhar a tabela do Ibovespa: mesmo havendo chave dupla, usar a coluna `id` comum.

## Alternativas consideradas

**Chave primária composta pela chave natural.** Rejeitada. Funciona, mas obriga o `IRepository<T>` genérico (que localiza por `id`) e o EF a lidar com chaves compostas em todo lugar, e amarra a identidade da linha a campos que podem precisar mudar.

**Chave natural como chave primária de coluna única** (por exemplo, o ticker). Rejeitada: não é única sozinha.

## Consequências

- O repositório genérico, as *specifications* e a convenção de nomes são os mesmos para todos os agregados.
- **A chave natural pode ser redefinida sem tocar na identidade da linha.** É a saída prevista para a colisão do `BIOM12F` (TDR-007): incluir o ISIN no índice único é uma migration de índice, não uma mudança de chave primária.
- Há um índice a mais por tabela (o da chave natural) além do da chave primária. Nas cotações (7,2 milhões de linhas), isso é o maior índice e é o que sustenta o *upsert* por diferença.
- A unicidade é garantida pelo banco, não só pelo código de importação: duas importações concorrentes do mesmo período não gravam duplicata, uma delas falha.
