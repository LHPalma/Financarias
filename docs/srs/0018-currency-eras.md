# SRS — Conversão das moedas antigas para real

- **Data:** 2026-10-11
- **Feature:** `Domain/MarketData/Currencies` + função SQL `to_reais`
- **Status:** **entregue.** O porquê da decisão está no `docs/adr/0002-original-currency-converted-on-read.md`; este documento registra o que foi construído e como se usa.
- **Fonte:** as leis de conversão monetária, conferidas contra o dólar do Banco Central e contra as cotações do COTAHIST nas trocas (`0007`, §10).

---

## 1. Introdução

### 1.1 Propósito
O Brasil trocou de moeda seis vezes entre 1970 e 1994, e todo dado histórico do projeto (ações, dólar, Ibovespa) vem na **moeda do dia**. Para comparar épocas, é preciso levar cada valor ao real. Este slice é a primeira metade disso; a segunda (poder de compra) é o valor presente, em `0011`.

### 1.2 Escopo
Dentro: a lista de eras, `ToReais(valor, data)` e a função `to_reais` do Postgres. Fora: correção de poder de compra (`0011`), moedas estrangeiras (`0001`) e qualquer data anterior a 15/05/1970.

## 2. As eras

| Moeda | De | Até | Unidades para R$ 1 | Código `MODREF` |
|---|---|---|---|---|
| Real | 01/07/1994 | (vigente) | 1 | `R$` |
| Cruzeiro Real | 01/08/1993 | 30/06/1994 | 2.750 | `CR$` |
| Cruzeiro | 16/03/1990 | 31/07/1993 | 2.750.000 | `CR$` |
| Cruzado Novo | 16/01/1989 | 15/03/1990 | 2.750.000 | `NCZ$` |
| Cruzado | 28/02/1986 | 15/01/1989 | 2.750.000.000 | `CZ$` |
| Cruzeiro (1970) | 15/05/1970 | 27/02/1986 | 2.750.000.000.000 | `CR$` |

## 3. Regras

### RN-01 — A moeda vem da data, nunca do código do arquivo
`CR$` serve a três períodos que valem coisas diferentes. A era de uma linha é a que vigorava no dia; o `MODREF` só **confere**: o parser aceita a linha se o código é o da moeda do dia (`IsLegalTenderOn`) e pula a que diverge.

### RN-02 — O Cruzado Novo começa em 16/01/1989
Não no dia 15. O dólar da série do Banco Central muda de escala entre 15 e 16 de janeiro de 1989, e é o que fixa a data.

### RN-03 — Data fora do intervalo é erro
Antes de 15/05/1970, `ToReais` lança `currency.date.unsupported`; a função SQL levanta `22008`. As conversões anteriores não foram validadas.

### RN-04 — `ToReais(valor, data) = valor / divisor da era`
Sem arredondamento: o preço de uma ação de Cr$ 2.020,00 em 1993 vale R$ 0,000735, e arredondar a centavos zeraria. O valor continua gravado na moeda do dia (ADR-002).

### RN-05 — A conversão troca a unidade, não o poder de compra
"Reais de hoje" combina esta conversão com o IPCA, em `to_present_value` (`0011`).

### RN-06 — Dois códigos, uma regra
O domínio (`BrazilianCurrency`) e o Postgres (`to_reais`) escrevem as mesmas faixas. Um teste de integração compara os dois lado a lado, nos dois lados de cada troca (13 pares data-valor).

## 4. Validação
- **O dólar do Banco Central** tem exatamente quatro saltos de 1000× ou mais (27/02→03/03/1986, 15→16/01/1989, 30/07→02/08/1993, 30/06→01/07/1994), e nenhum nas trocas de 1 por 1.
- **Em cada troca, a razão mediana** do preço por ação em reais entre os dois pregões vizinhos é de ordem 1 (1,200 na troca Cruzeiro→Cruzado, com 266 papéis; 0,968 em Cruzado→Cruzado Novo, com 194), sem nenhum salto de 1000×. A tabela completa está em `0007`.
- **O dólar em reais constantes** de 1984 a 1994 fica numa faixa estreita (R$ 10 a R$ 19) quando se aplica a conversão e o índice de inflação (`0011`, §5).

## 5. Rastreabilidade
| Commit | Entrega |
|---|---|
| `928a09d` | Eras e conversor no domínio |
| `8d25f1e` | Função `to_reais` no Postgres |
| `8703213` | Parser do COTAHIST aceita as moedas antigas |
| `7ce283b` | ADR-002 |
| `8ae2541`, `4157180` | SRS: as cotações antigas e a conferência da conversão |

## 6. Limites conhecidos
- Antes de 1970 não há conversão.
- A conferência de 1990 (Cruzado Novo → Cruzeiro) usou só 2 papéis; é a troca menos testada.
- Os saltos que a conversão não explica em 1986–1994 são de outra natureza (mudança de lote sem `quote_factor`, eventos que a B3 não publica) e estão no TDR-006.
