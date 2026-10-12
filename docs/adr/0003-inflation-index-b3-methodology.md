# ADR-003 — Índice de inflação diário pela metodologia da B3

- **Data:** 2026-10-11
- **Status:** aceita
- **Commits:** `24c342c` (índice no domínio), `3c85da3` (agregado), `4a0bc30` (persistência e `to_present_value`), `2decb30` (reconstrução)
- **Relacionada:** `docs/srs/0011-present-value.md`, `docs/adr/0002-original-currency-converted-on-read.md`

---

## Contexto

Trazer um valor de 1990 a reais de 2026 exige um índice de preços, e o IBGE só publica o IPCA **por mês**. O estudo de eventos precisa de um nível para **cada dia** (um dia de pregão em 15 de outubro e outro em 28 de outubro não podem receber o mesmo deflator). Havia quatro formas de passar do mensal ao diário, e elas dão resultados diferentes.

## Decisão

**O índice diário segue o *Manual de Curvas* da B3: o fator de cada mês é espalhado geometricamente pelos dias úteis entre dois dias 15 ajustados.**

- O "15 ajustado" é o dia 15, ou o próximo dia útil se o 15 não for útil.
- O fator do mês `M` leva o índice do 15 ajustado de `M` ao 15 ajustado de `M+1`: `Nível(d) = Nível(início) × fator^(DU(início, d] / DU(início, fim])`.
- Dia não útil repete o nível do dia útil anterior.
- O índice vale de `1980-01-15` (nível 1) até o 15 ajustado seguinte ao último IPCA publicado.

Como isso depende do calendário, o histórico de feriados teve de recuar a 1980 (BrasilAPI, `0015`).

## Alternativas consideradas

**Nível do mês constante no mês inteiro (degraus).** Rejeitada. É a mais simples, mas faz o deflator saltar no dia 1 e ficar parado nos outros 29, o que cria um ruído que não existe no preço.

**Interpolação linear em dias corridos entre os meses.** Rejeitada. Inflação acumula de forma composta, não linear, e o erro cresce nos períodos de inflação alta (1986–1994), justamente os que mais interessam. A forma geométrica é a consistente com como o IPCA é composto.

**Alinhar ao período de coleta do IBGE** (o índice de `M` fica completo no fim de `M`). Rejeitada em favor da convenção do mercado. O IBGE coleta o preço durante o mês `M` inteiro, enquanto a B3 atinge o índice de `M` só no dia 15 de `M+1`, o que deixa a série **um mês atrasada** em relação ao período de coleta. A escolha é do usuário, por ser a convenção que o mercado usa nas curvas, e foi aceita com o atraso conhecido.

**A projeção de VNA da ANBIMA (pró-rata em dias corridos entre dias 15, sem feriados).** Não aplicável. É a metodologia **do título público** (`0016`), correta para o VNA da NTN-B, e é outra regra, para outro uso. O índice diário é uma curva de mercado, e a B3 usa dias úteis.

## Consequências

- A série fica **um mês atrasada** em relação ao período de coleta. Quem precisar do alinhamento do IBGE troca a regra em `InflationIndex`; não deve corrigir na leitura.
- O resultado depende do calendário de feriados. Em 1986–1990 a BrasilAPI aplica a lei de hoje e erra um ou dois dias úteis por ano (TDR-005).
- O custo é pequeno: 17.076 níveis (um por dia corrido de 1980 a 2026).
- O IPCA do mês corrente, ainda não publicado, não entra: o último nível é o 15 ajustado seguinte ao último mês publicado.
- Os testes do domínio conferem a propriedade que define a regra (a janela entre dois 15 ajustados fecha exatamente em `nível × fator`), e a série do dólar em reais constantes de 1984 a 1994 serviu de teste de plausibilidade: ficou numa faixa estreita, com quebras só nos planos econômicos.
