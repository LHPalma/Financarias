# TDR-004 — `inflation_levels` não se atualiza quando o IPCA ou os feriados mudam

- **Data:** 2026-10-11
- **Status:** aceito (dívida assumida deliberadamente)
- **Commits:** `4a0bc30`, `2decb30`, `a1a0ae6`
- **Quadrante (Fowler):** deliberado + prudente — a tabela é um derivado explícito (ADR-005) e a reconstrução manual foi a escolha mais simples

---

## Contexto

O índice de inflação diário é derivado do IPCA mensal (`indicator_values`) e do calendário de feriados (`holidays`), e é gravado em `inflation_levels` para que o SQL o consuma (`to_present_value`). Importar um mês novo de IPCA, ou feriados novos, não toca nessa tabela.

## Débito assumido

A tabela só muda quando alguém roda a mutation `rebuildInflationLevels`. Não há gatilho, tarefa agendada nem aviso. Entre uma importação e a reconstrução, `to_present_value` devolve valores consistentes com o dado **antigo**, sem sinal de que está velho.

## Impacto

- Um mês de IPCA novo não aparece no índice, e o último nível continua o 15 ajustado do mês anterior.
- Um feriado importado depois da reconstrução (por exemplo, a correção de 1986–1990) não muda os níveis até a próxima.
- O erro é silencioso: nenhuma consulta falha.

## Alternativa correta (não implementada)

Reconstruir como **efeito** das importações que a alimentam: `importIndicatorValues` para o IPCA e `importHolidays` encadeiam a reconstrução (ou publicam um evento que a dispara), ou um gatilho de banco marca a tabela como "suja" e `to_present_value` recusa consulta enquanto estiver suja. A primeira é a mais barata (a reconstrução leva segundos) e mantém a regra no domínio.

## Quando resolver

Quando a atualização passar a ser rotina (um IPCA novo por mês, entrando por uma agenda — ver ADR-006). Antes disso, vale uma linha no roteiro de carga (`docs/runbook-carga-de-dados.md`): depois de importar IPCA ou feriados, rodar `rebuildInflationLevels`.
