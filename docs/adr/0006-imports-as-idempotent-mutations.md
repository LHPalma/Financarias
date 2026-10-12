# ADR-006 — Importações como mutations GraphQL idempotentes, e não como tarefas agendadas

- **Data:** 2026-10-11
- **Status:** aceita
- **Commits:** o padrão nasce em `841d915` (`importHolidays`) e se repete em `f12c5a2` (`importDailyQuotes`), `661cfb4` (`importIndexQuotes`), `cd026a3` (`importIndicatorValues`), `d190e08` (`importCorporateEvents`), `a1a0ae6` (`rebuildInflationLevels`) e `fc52f0b` (`importIssuers`)
- **Relacionada:** `docs/debt/0002-public-maintenance-mutations.md`

---

## Contexto

O projeto tem um armazém de dados de mercado alimentado por fontes externas heterogêneas (arquivos anuais da B3, séries do Banco Central, planilhas, páginas). Cada carga é eventual e guiada por uma pergunta ("preciso de 2014 a 2022", "falta o minério"), não por um relógio. Era preciso decidir como disparar e repetir uma carga.

## Decisão

**Cada carga é uma mutation GraphQL, sobre um caso de uso e um `CommandHandler`, e é idempotente por chave natural.**

- Repetir a mesma carga não duplica nada: o handler lê o que já existe pelo repositório, compara e grava só a diferença. O resultado devolve contadores (`rowsRead`, `created`, `updated`, `removed`, `skipped`) que dizem o que a repetição mudou.
- Eventos corporativos usam uma comparação de **multiconjunto**: parcelas idênticas de uma mesma data contam uma a uma, e o que a fonte deixou de publicar sai.
- Falha parcial de uma fonte não derruba o resto quando é possível seguir (um ano da BrasilAPI que falha é registrado e pulado).
- O tempo de execução de uma requisição GraphQL foi estendido para 10 minutos, o suficiente para a maior carga (um ano de COTAHIST).
- A carga completa é um laço de chamadas (um `curl` por ano), que mora fora do repositório.

## Alternativas consideradas

**Um serviço em segundo plano (`BackgroundService`, Hangfire, Quartz) com agenda.** Rejeitada por ora. Resolve o problema de manter o dado fresco, que não é o problema atual: as cargas são históricas e guiadas por pergunta. Um agendador traz estado, política de repetição, observabilidade e um processo a mais, tudo antes de haver uma série que precise de atualização diária.

**Uma ferramenta de linha de comando separada.** Rejeitada. Duplicaria o *composition root*, a configuração e os casos de uso, e perderia a documentação de contrato que o schema GraphQL já dá a cada operação.

**Importar no *startup* da aplicação.** Rejeitada. Mistura o boot com uma operação lenta e dependente de rede.

## Consequências

- A carga é reproduzível e retomável: se cair no meio, repete-se. Foi o que permitiu corrigir o parser do COTAHIST e reimportar 1998 e 2000 vendo, pelo contador, o que a correção destravou.
- As mutations são operações **pesadas e, hoje, públicas**: qualquer chamador as dispara (TDR-002). Cada uma diz isso no schema.
- Cada carga vira muitas chamadas; o limite de 10 minutos é um teto, não uma garantia.
- Manter o dado atualizado continua manual. Quando houver uma série que precise de frescor diário, o agendador entra **por cima** destes mesmos casos de uso, sem reescrevê-los.
- Pelo mesmo motivo de reprodutibilidade, os testes de integração conferem a idempotência: uma segunda execução devolve zero criados, zero atualizados.
