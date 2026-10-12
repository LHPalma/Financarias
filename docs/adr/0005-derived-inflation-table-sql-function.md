# ADR-005 — Índice de inflação gravado numa tabela derivada e consumido por uma função SQL

- **Data:** 2026-10-11
- **Status:** aceita
- **Commits:** `4a0bc30` (tabela e `to_present_value`), `2decb30` (reconstrução), `27dfc58` (arredondamento do resultado)
- **Relacionada:** `docs/adr/0003-inflation-index-b3-methodology.md`, `docs/adr/0002-original-currency-converted-on-read.md`, `docs/srs/0011-present-value.md`

---

## Contexto

O valor presente (`ToReais(valor, data) × Nível(base) / Nível(data)`) é consumido principalmente **dentro de consultas SQL**: os relatórios cruzam milhões de linhas de cotação e de volume com o deflator de cada dia. O índice, porém, nasce de uma regra com dias úteis, feriados e potências, escrita no domínio em C#. Era preciso decidir onde o nível de cada dia vive.

## Decisão

**O índice é calculado pelo domínio (`InflationIndex`), gravado dia a dia em `inflation_levels` (um nível por dia corrido) e consumido pela função SQL `to_present_value(valor, dia, dia_base)`.**

- A mutation `rebuildInflationLevels` recalcula tudo e grava **só a diferença** (cria, atualiza, remove).
- `to_present_value` é `plpgsql`, `STABLE`; sem data-base usa o último dia do índice; dia sem nível levanta `22008`.
- O nível é guardado com 16 casas (`numeric(38,16)`, passa de 10¹²), e o **resultado** da função é arredondado a 8 casas.

## Alternativas consideradas

**Calcular o índice a cada consulta, em C#.** Rejeitada. Obrigaria toda análise a sair do SQL, trazer as linhas para a aplicação e calcular lá. Um estudo que filtra e agrega 7 milhões de cotações não cabe nesse formato.

**Reimplementar a regra inteira em SQL (função ou *view*).** Rejeitada. A regra de dias úteis entre dias 15 ajustados, com potência fracionária e feriados de duas fontes, em SQL puro, seria uma segunda implementação para manter igual à do domínio. A tabela guarda o **resultado** da regra, escrita uma vez.

**Visão materializada.** Rejeitada pelo mesmo motivo (a regra continuaria em SQL), mais o refresh, que também seria manual.

**Gatilho no banco para recalcular quando o IPCA ou os feriados mudam.** Rejeitada por ora. Esconderia uma operação de ~17 mil linhas dentro de uma importação, e o gatilho dependeria da mesma regra em SQL. Fica como a evolução natural (TDR-004).

## Consequências

- **A tabela é derivada e pode ficar velha**: importar IPCA ou feriados novos não a atualiza. É preciso rodar `rebuildInflationLevels` (TDR-004).
- A regra existe em um só lugar (o domínio); o SQL só lê o resultado. Um teste de Postgres confere `to_present_value` com o `InflationIndex` do domínio.
- O resultado da função é arredondado a 8 casas **porque o `numeric` sem limite devolve mais de 28 algarismos significativos e o Npgsql não consegue ler como `decimal`** (`OverflowException`). Foi descoberto pelo teste de Postgres e corrigido numa segunda migration (`27dfc58`). Quem usar a função por fora do .NET não precisa do arredondamento, mas ele não atrapalha.
- O padrão é o mesmo da conversão de moedas: o domínio é a fonte da regra, o SQL é espelho (`to_reais`) ou consumidor (`to_present_value`).
