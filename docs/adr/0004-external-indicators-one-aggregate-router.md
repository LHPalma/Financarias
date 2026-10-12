# ADR-004 — Indicadores externos no mesmo agregado, atrás de um roteador, de fontes sem chave

- **Data:** 2026-10-11
- **Status:** aceita
- **Commits:** `7f3888e` (enum), `3f77999` (tentativa com a FRED), `067e6e4` (troca pelas fontes sem chave), `be754e0` (nova tentativa no Fed)
- **Relacionada:** `docs/srs/0012-external-indicators.md`, `docs/srs/0009-bcb-indicators.md`

---

## Contexto

Os estudos de evento pediam contexto externo: Brent, minério de ferro, índice do dólar e VIX. O projeto já tinha `IndicatorValue` (indicador, data, valor), uma porta `IIndicatorValueProvider` e a mutation `importIndicatorValues`, tudo desenhado para o Banco Central. Havia duas perguntas: onde guardar os valores novos e de onde buscá-los.

## Decisão

1. **Os valores entram no mesmo agregado e na mesma tabela** (`indicator_values`), com cinco valores novos em `EconomicIndicator`.
2. **`IndicatorValueProviderRouter` é a implementação registrada da porta** e delega ao provedor de cada indicador (Banco Central, EIA, Cboe, Fed, Banco Mundial). O handler de importação e os testes dele não mudam.
3. **Só fontes que não pedem chave nem cadastro.** Brent e WTI vêm de planilhas da EIA, o VIX do CSV da Cboe, o índice do dólar do relatório H.10 do Fed e o minério da planilha *Pink Sheet* do Banco Mundial.

## Alternativas consideradas

**Um agregado e uma tabela por fonte.** Rejeitada. O formato é idêntico (indicador, data, valor), a chave natural é a mesma e a mutation e o diff de importação já existem. Um agregado por fonte duplicaria tudo para separar apenas a origem, que o enum já distingue.

**Um método `Supports(indicator)` na porta, com o handler escolhendo entre vários provedores.** Rejeitada. Obrigaria o handler a conhecer a pluralidade de fontes e quebraria os testes que simulam a porta (um `Substitute` devolve `false` por padrão). O roteador mantém a porta como estava e concentra a escolha em um `switch` que o compilador ajuda a manter completo.

**FRED (Federal Reserve Bank of St. Louis) como fonte única.** Foi a primeira implementação, e é a mais limpa: cinco séries, um formato. Foi abandonada por duas razões, medidas:
  - O endpoint público de CSV **reinicia a conexão de clientes .NET** (testado com várias combinações de `User-Agent`, cabeçalhos e versão de HTTP; só passam agentes de ferramentas conhecidas, como `curl` e `wget`).
  - A API oficial exige uma chave.

**Fingir ser `curl` ou `wget`.** Rejeitada. Seria contornar a proteção contra robôs do serviço, e não uma correção.

**FRED pela API oficial, com chave.** Rejeitada por ora. Obriga a um cadastro e a um segredo novos para uma carga de uso eventual, e as fontes sem chave existem. Continua sendo a saída se alguma delas sair do ar.

## Consequências

- Acrescentar um indicador é um valor de enum e uma linha no roteador; um provedor novo é uma classe.
- São **quatro fontes sem contrato publicado**: qualquer uma pode mudar de formato. Um teste de contrato por indicador (`Integrations.IntegrationTests`, semanal) é o aviso.
- Cada fonte tem sua falha: o Fed devolve, às vezes, corpo vazio com status 200 (por isso o provedor exige o cabeçalho da série e tenta de novo; sem isso a carga parecia certa e 14 de 21 anos vieram vazios); o endereço do Pink Sheet muda a cada edição (o provedor lê o link na página).
- Cada chamada de um ano baixa o arquivo inteiro da fonte (TDR-010).
- A resiliência é feita provedor a provedor, não em um lugar (TDR-011).
