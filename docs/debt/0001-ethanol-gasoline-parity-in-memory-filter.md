# TDR-001 — Filtro de `ethanolGasolineParity` roda em memória, não vira `WHERE` no Postgres

- **Data:** 2026-08-30
- **Status:** aceito (dívida assumida deliberadamente)
- **Commits:** `d5f00de`
- **Quadrante (Fowler):** deliberado + prudente — decisão consciente, escolhida sabendo o custo, pra não bloquear a entrega da fatia

---

## Contexto

O resolver `ethanolGasolineParity` precisa achar, por posto, o preço mais recente de etanol e de gasolina, casar os dois e calcular a razão. Essa forma de consulta (agrupar por posto+produto, pegar o mais recente, casar com o produto irmão, atravessar a navegação pro posto) não traduz pro SQL do Postgres via LINQ do EF Core — um `GroupBy` duplo com projeção de navegação lança `ProjectionBindingExpression ... could not be translated` em tempo de execução (não de compilação), verificado contra Postgres real via Testcontainers.

A correção adotada foi materializar as linhas cruas (`Include` + `ToListAsync`) e fazer os dois agrupamentos em memória, com LINQ-to-Objects. Isso resolveu a tradução, mas criou um efeito colateral: pra oferecer `where`/`orderBy` no GraphQL (Style B, `[UseFiltering]/[UseSorting]`), a saída do handler virou um `.AsQueryable()` sobre essa lista **já materializada**.

## Débito assumido

O filtro do cliente (`where: { state: { eq: "SP" } }`) funciona corretamente do lado do GraphQL, mas **não chega no SQL**. Toda chamada busca a fatia inteira de `FuelPrice` (produtos Ethanol/Gasoline) do banco — hoje ~138 mil linhas brutas, ~69 mil resultados finais — antes de qualquer filtro ser aplicado. O filtro só decide o que descartar depois, em memória, no processo da API.

## Impacto

- **Custo por chamada:** ~138 mil linhas + `JOIN` com `fuel_stations` são lidas do Postgres a cada execução, independente do quão restritivo for o filtro do cliente.
- **Não escala com o filtro:** pedir 1 posto específico custa o mesmo que pedir todos.
- **Cresce com a base ANP:** o volume sobe a cada novo semestre importado; hoje é tolerável, mas não é um teto fixo.
- **Sem risco de corretude:** o resultado retornado é correto — o problema é só custo, não comportamento errado.

## Alternativa correta (não implementada)

Reescrever a consulta sem `GroupBy`, usando subquery correlacionada com `MAX(collected_on)` por posto+produto (padrão "greatest-n-per-group", bem suportado pelo EF) e um `JOIN` entre a linha de etanol e a de gasolina do mesmo posto — mantendo tudo como `IQueryable` sem materializar. O cálculo da razão e o limiar de 70% precisariam ser escritos **inline** na LINQ (`ethanol.SalePrice / gasoline.SalePrice`), porque o EF não traduz uma chamada ao método `EthanolGasolineParity.Ratio`/`IsEthanolAdvantageous` do domínio — custo extra: a fórmula fica duplicada entre o domínio (usado nos testes) e a query.

## Quando resolver

Se este endpoint virar consumo frequente (dashboard, polling, client público de verdade) — hoje é uso pontual/exploratório. Reavaliar também se a base ANP crescer o suficiente pra "~138 mil linhas por chamada" deixar de ser tolerável.
