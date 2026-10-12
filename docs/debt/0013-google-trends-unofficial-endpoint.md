# TDR-013 — O Google Trends é lido por um endpoint não oficial

- **Data:** 2026-10-11
- **Status:** aceito (dívida assumida deliberadamente)
- **Commits:** `fe2a47a` (provedor), `df38c58` (persistência), `0414228` (mutation)
- **Quadrante (Fowler):** deliberado + prudente — o risco é conhecido, o volume é mínimo e o dado importado fica guardado, então uma queda da fonte não perde o que já foi coletado

---

## Contexto

A pergunta sobre a Americanas pedia dado de busca diário por termo. O Google Trends é a fonte natural, mas o acesso programático oficial é restrito: o site público usa três chamadas internas (`explore`, que entrega o token, e `widgetdata/multiline`, que entrega a série), precedidas de uma visita à página para obter o cookie de sessão.

## Débito assumido

O provedor usa essas chamadas internas, sem contrato. A resposta traz, no próprio corpo, `"userType": "USER_TYPE_SCRAPER"`: o Google sabe que o cliente é automatizado e, por ora, atende.

## Impacto

- O Google pode mudar o formato, exigir outro cabeçalho ou bloquear (`429`) sem aviso. Antes de existir a sessão por cookie, a mesma consulta já respondia `429`.
- Os termos de uso do serviço podem não permitir consulta automatizada. O volume é de poucas requisições por carga, e não há rotina agendada.
- O índice é amostrado (duas coletas diferem em até 1 ponto) e relativo ao grupo e ao intervalo (SRS 0019, RN-01 e RN-05).

## Alternativa correta (não implementada)

Usar o acesso oficial quando ele estiver disponível para o projeto (a Google Trends API, em acesso restrito), ou um provedor contratado de séries de busca. Como a porta `ISearchInterestProvider` isola a fonte, a troca é um provedor novo, sem mexer em domínio, caso de uso ou banco.

## Quando resolver

Se a carga virar rotina (agendada, em lote, ou em produção), ou se a fonte começar a bloquear. Enquanto o uso for eventual, guiado por pergunta e limitado a poucas requisições, vale como está: o dado já importado continua no banco.
