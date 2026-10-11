# ADR-002 — Guardar o valor na moeda original e converter para reais na leitura

- **Data:** 2026-10-11
- **Status:** aceita
- **Commits:** `928a09d` (eras e conversor no domínio), `8d25f1e` (função `to_reais` no Postgres), `8703213` (parser aceita as moedas antigas)
- **Relacionada:** `docs/srs/0007-b3-cotahist.md` (RN-15), `docs/srs/0009-bcb-indicators.md` (RN-03)

---

## Contexto

O Brasil trocou de moeda seis vezes entre 1970 e 1994, e o dado histórico que o projeto lê vem na moeda do dia. Até julho de 1994 o COTAHIST rejeitava tudo que não fosse `R$` (`0007`, RN-15), e por isso o banco só tinha cotações depois de 04/07/1994. Quando o usuário pediu os anos do Collor e do pré-Plano Real, a pergunta passou a ser como guardar preços em cruzeiros, cruzados e cruzeiros reais de modo que uma série longa faça sentido.

Três fatos medidos nos dados decidiram:

| Fato | Medição |
|---|---|
| As conversões são **exatas e fixas**: 1 R$ = 2.750 CR$ = 2.750.000 Cr$ = 2,75 × 10⁹ Cz$ = 2,75 × 10¹² Cr$ (1970) | O dólar do BCB tem exatamente quatro saltos de 1000× ou mais (27/02 a 03/03/1986, 15 a 16/01/1989, 30/07 a 02/08/1993, 30/06 a 01/07/1994), e nenhum nas trocas de 1 por 1 |
| O código de moeda do arquivo é **ambíguo** | `CR$` aparece para o Cruzeiro de 1970 a 1986, para o Cruzeiro de 1990 a 1993 e para o Cruzeiro Real de 1993 a 1994, e esses três valem coisas diferentes |
| Em reais, o preço antigo é **minúsculo** | Uma ação de Cr$ 2.020,00 em 1993 vale R$ 0,000735; o dólar de 1984 vale R$ 1,03 × 10⁻⁹. A coluna `numeric(13,2)` zeraria isso |

## Decisão

**O valor é gravado como a fonte o publica, na moeda do dia, e a conversão para reais é feita na leitura, por uma função da data.**

- O domínio tem `BrazilianCurrency` (`Domain/MarketData/Currencies`), com a lista de eras (`CurrencyEra`: nome, primeiro e último dia, divisor para reais e código `MODREF`) e `ToReais(valor, data)`.
- O Postgres tem a função `to_reais(valor, data)`, escrita nas mesmas faixas, para as análises em SQL. Um teste confere as duas implementações lado a lado, nos dois lados de cada troca.
- A moeda de uma linha vem da **data**, não do código do arquivo. O código serve de conferência: o parser só aceita a linha se o `MODREF` for o da moeda que vigorava no dia, e pula em silêncio a que divergir.
- O Cruzado Novo começa em **16/01/1989**, e não no dia 15: o dólar da série do Banco Central muda de escala entre esses dois dias.
- Datas anteriores a 15/05/1970 são recusadas (`currency.date.unsupported`), porque as conversões anteriores não foram validadas.

## Alternativas consideradas

**Converter para reais na importação e gravar só o valor em reais.** Rejeitada. O preço de 1993 viraria 0,0007, e as colunas `numeric(13,2)` perderiam quase todo o valor; alargar as colunas para muitas casas decimais mudaria a tabela inteira para acomodar um período de 8 anos. Além disso o dado deixaria de ser o que a B3 publica: ninguém poderia conferir a linha contra o arquivo. A conversão é determinística, então fazê-la na leitura não custa fidelidade.

**Gravar uma coluna de moeda lida do `MODREF`.** Rejeitada. Como `CR$` é ambíguo, a coluna não resolveria a conversão, e a data é suficiente e vem sempre preenchida. Uma coluna a mais seria dado derivado que poderia divergir da data.

**Guardar o divisor por linha.** Rejeitada pelo mesmo motivo: derivado da data, e redundante em 6 milhões de linhas.

**Limitar o banco às cotações em reais.** Era a situação anterior. Funciona, mas deixa de fora nove anos de história, incluindo os planos Cruzado, Collor e Real.

## Consequências

- Qualquer consulta que compare períodos precisa usar `to_reais` (ou `BrazilianCurrency.ToReais`); olhar o preço cru de 1993 junto com o de 2000 é um erro silencioso. A função e o conversor existem para isso, e o preço cru continua disponível para conferir contra a fonte.
- O retorno entre dois dias da mesma era não precisa de conversão; atravessar uma troca de moeda sem converter produz um salto de 1000× ou mais.
- A conversão troca a **unidade**, não o **poder de compra**. "Reais de hoje" é outra conta, que combina a conversão com o IPCA, e é o passo seguinte.
- Os fatores vêm de leis de conversão e estão validados pela série do dólar. Se a B3 ou o Banco Central corrigirem a data de uma troca, o teste de continuidade do dólar é o que avisa.
