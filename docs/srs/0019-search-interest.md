# SRS — Interesse de busca (Google Trends)

- **Data:** 2026-10-11
- **Feature:** `Domain/MarketData/SearchInterest` + `Application/MarketData/SearchInterest` + `Integrations/MarketData/Google/Trends`
- **Status:** **entregue**, exceto a query de leitura. Agregado, tabela `search_interest_points`, importação e a mutation `importSearchInterest` estão em `main`, com carga real (§5).
- **Fonte externa:** a interface pública do [Google Trends](https://trends.google.com), **sem chave e sem contrato**. Ver `docs/debt/0013-google-trends-unofficial-endpoint.md`.

---

## 1. Introdução

### 1.1 Propósito
Medir atenção do público a um tema. Nasceu da pergunta "os clientes da Americanas migraram para os concorrentes?" (relatório de eventos, aba Americanas): preço de ação mede expectativa, e a busca é um dado de comportamento. É um sinal de **atenção**, não de compra.

### 1.2 Escopo
Dentro: importar a série diária de 1 a 5 termos num país e num intervalo, guardar e repetir sem duplicar. Fora: série semanal ou mensal (intervalos acima de 270 dias), termos relacionados, consultas por região dentro do país além do código de região, e a query GraphQL de leitura.

## 2. Regras

### RN-01 — O índice é relativo ao grupo e ao intervalo
O Google devolve de 0 a 100, em que **100 é o maior valor entre todos os termos e todos os dias da consulta**. Pedir um termo a mais, ou um intervalo diferente, muda a escala de todos. Por isso a chave natural é `(país, grupo de termos, início, fim, termo, dia)`, e só se comparam valores de um mesmo grupo e intervalo. Guardar só `(termo, dia)` misturaria escalas incomparáveis.

### RN-02 — O grupo é uma chave canônica
`ComparisonKey` é o conjunto de termos normalizado (minúsculas, sem espaços nas pontas, sem repetidos), ordenado e unido por `|`. Pedir `["magalu", "Americanas"]` ou `["americanas", "magalu"]` dá o mesmo grupo.

### RN-03 — Limites validados no domínio e no banco
- 1 a 5 termos distintos; termo de 1 a 100 caracteres.
- País em duas letras maiúsculas, com região opcional (`BR`, `BR-SP`).
- Início ≤ fim, e **no máximo 270 dias**: é o limite em que o Google ainda devolve série diária; acima disso devolve semanas, e a série deixaria de ser diária sem aviso.
- Valor de 0 a 100 e dia dentro do intervalo, também como `CHECK` no banco.
Violação lança `searchinterest.point.invalid`.

### RN-04 — O último dia parcial não é gravado
Quando o intervalo chega a hoje, o Google marca o último ponto como parcial. Ele ainda muda, então é descartado.

### RN-05 — Repetir é seguro, e a fonte amostra
A importação lê o que já existe para o grupo, o intervalo e o país, cria o que falta e atualiza o que mudou. O Google **amostra** os dados: duas coletas do mesmo intervalo diferiram em até 1 ponto em 20 de 455 valores. Uma repetição pode, portanto, devolver `pointsUpdated > 0` sem que nada de relevante tenha mudado.

### RN-06 — Sessão antes da consulta
O Google responde `429` a quem consulta sem passar antes pela página (que entrega o cookie de sessão). O provedor abre a sessão e usa o mesmo cookie nas duas chamadas seguintes (`explore`, que dá o token, e `widgetdata/multiline`, que dá a série). O `User-Agent` é o do projeto (`Financarias/1.0`), sem se passar por navegador.

## 3. Camadas
```
importSearchInterest → IImportSearchInterestUseCase : normaliza termos, valida grupo, país e intervalo
                     → ICommandHandler<ImportSearchInterestCommand, SearchInterestImportResult>
                     → ISearchInterestProvider (porta) ← GoogleTrendsSearchInterestProvider ← IGoogleTrendsClient (Refit)
                     → IRepository<SearchInterestPoint> + SearchInterestByGroupSpecification
```

## 4. Superfície GraphQL
`importSearchInterest(input: { terms, geo, from, to })` devolve `{ pointsRead, pointsCreated, pointsUpdated }`. Manutenção, sem autenticação hoje. Cada termo traz um ponto por dia: 5 termos em 91 dias dão 455 pontos.

## 5. Carga e validação (2026-10-11)
Duas cargas, ambas com `americanas`, `magalu`, `mercado livre`, `shopee` e `casas bahia` no Brasil: 01/12/2022 a 01/03/2023 (a janela do caso Americanas) e 01/12/2021 a 01/03/2022 (a mesma janela um ano antes, como linha de base sazonal). 455 pontos cada. O pico de `americanas` cai em **12/01/2023**, o dia seguinte ao comunicado, com valor 100.

Testes: domínio (normalização, chave do grupo, limites), caso de uso e handler (cria, atualiza, repete), provedor com respostas reais capturadas (ponto parcial, widget ausente, resposta que não é JSON), persistência em Postgres (escala por intervalo, unicidade, `CHECK`), funcional da mutation e um teste de contrato contra o Google real, fora do CI principal.

## 6. Limites conhecidos
- **Endpoint não oficial**, que o próprio Google classifica como `USER_TYPE_SCRAPER` na resposta. Pode bloquear ou mudar a qualquer momento (TDR-013).
- Busca não é compra, e termos ambíguos (como "americanas", que também é o gentílico) misturam significados.
- Sem query de leitura GraphQL; o relatório lê por SQL.
