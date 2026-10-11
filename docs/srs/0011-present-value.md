# SRS — Valor presente (índice de inflação diário e `to_present_value`)

- **Data:** 2026-10-11
- **Feature:** `Domain/MarketData/Inflation` + `Application/MarketData/Inflation` + função SQL `to_present_value`
- **Status:** **entregue.** Índice no domínio, tabela `inflation_levels`, função SQL, reconstrução por mutation e carga real (1980-01-15 a 2026-10-15) estão em `main`.
- **Fonte:** o IPCA mensal já importado (`0009`) e o calendário de feriados (`Anbima` a partir de 2001, `BrasilAPI` de 1980 a 2000). Nenhuma fonte nova de inflação.
- **Metodologia:** *Manual de Curvas da B3* — pro rata geométrico por dias úteis entre dois dias 15 ajustados.

---

## 1. Introdução

### 1.1 Propósito
Dar a um valor do passado, **na moeda da época**, o seu equivalente em reais de uma data-base. Duas etapas, nesta ordem:

1. **Converter para real** (`BrazilianCurrency.ToReais`, ADR-002): o cruzeiro de 1985 vira reais pelos fatores legais de cada troca de moeda.
2. **Trazer a valor presente**: multiplicar pela razão `Nível(base) / Nível(data)` do índice de inflação diário.

```
ValorPresente = ToReais(valor, data) × Nível(base) / Nível(data)
```

É o que deixa comparar a queda de um dia de 1990 com a de um dia de 2018 sem que a moeda e a inflação decidam o resultado.

### 1.2 Escopo
- Índice diário construído do IPCA mensal, persistido em `inflation_levels` (um nível por dia corrido).
- Função SQL `to_present_value(valor, dia, dia_base)`.
- Mutation `rebuildInflationLevels`, que recalcula e grava só a diferença.

**Fora de escopo:** IPCA diário real (não existe — o IBGE só publica o mensal), outros índices (IGP-M, INPC), projeção do IPCA do mês corrente, query GraphQL de valor presente.

## 2. Regras de negócio

### RN-01 — O índice é diário, mas a fonte é mensal
O IBGE só publica o IPCA do mês. O índice diário é uma **interpolação**, e a convenção é o que muda o resultado. Adotou-se a da B3 (RN-02), que é a que o mercado usa para as curvas.

### RN-02 — Metodologia da B3: dias úteis entre dois dias 15 ajustados
- O fator do mês `M` é espalhado **geometricamente** pelos dias úteis entre o dia 15 ajustado de `M` e o dia 15 ajustado de `M+1`.
- **Dia 15 ajustado:** o dia 15; se não for dia útil, o próximo dia útil (`AddBusinessDays(14, 1)`).
- `Nível(d) = Nível(início) × fator^(DU(início, d] / DU(início, fim])`, e no fim da janela vale exatamente `Nível(início) × fator`.
- Dias não úteis dentro da janela repetem o nível do dia útil anterior (não há dia útil novo para contar).
- `FirstDate` é o 15 ajustado do primeiro mês da série (nível 1); `LastDate` é o 15 ajustado seguinte ao último mês.

### RN-03 — Defasagem de um mês, aceita
Pela B3, o índice do mês `M` é atingido no dia 15 de `M+1`. O IBGE coleta o IPCA de `M` durante `M` inteiro, então a série fica **um mês atrasada** em relação ao período de coleta. O usuário aceitou a convenção da B3 por ser a do mercado; quem precisar do alinhamento pelo período de coleta deve trocar a regra em `InflationIndex`, não corrigir na leitura.

### RN-04 — Feriados decidem o resultado
Dia útil depende do calendário. Duas fontes, cobrindo períodos diferentes:

| Período | Fonte | Observação |
|---|---|---|
| 2001 em diante | Anbima (planilha) | vem primeiro quando há data repetida |
| 1980 a 2000 | BrasilAPI (regras de calendário) | só nacionais; a Páscoa é ignorada; feriados em fim de semana são mantidos |

**Ressalva 1986–1990:** a BrasilAPI aplica as regras *de hoje* ao passado. O período de 1986 a 1990 teve feriados e pontes que nenhuma regra atual reproduz, então os dias úteis daqueles anos podem estar errados em um ou dois dias por ano. O efeito sobre o nível é de segunda ordem (um dia útil a mais ou a menos numa janela de ~21), mas existe.

### RN-05 — Data-base padrão
`to_present_value` sem data-base usa o **último dia** do índice (hoje, 2026-10-15). Passar a data-base explícita fixa o resultado no tempo.

### RN-06 — Datas fora do índice são erro
Dia (ou base) sem nível levanta `22008` no Postgres e `inflation.date.unavailable` no domínio. Não se extrapola: antes de 1980-01-15 não há IPCA, e depois do último 15 ajustado a fonte ainda não publicou.

### RN-07 — O índice gravado é derivado
`inflation_levels` é cache de `IPCA × feriados`. **Não se atualiza sozinho:** importar um novo mês de IPCA ou novos feriados deixa a tabela velha até alguém rodar `rebuildInflationLevels`. A mutation grava só a diferença (cria, atualiza, remove), então repetir sem mudança nos dados não altera nada.

### RN-08 — Precisão
O nível passa de 10¹² (acumulado desde 1980) e é guardado com 16 casas (`numeric(30,16)`, arredondado `AwayFromZero`). `to_present_value` arredonda o **resultado a 8 casas**: sem isso o produto de divisões do `numeric` tem mais de 28 algarismos significativos e o Npgsql não consegue ler como `decimal` (`OverflowException`).

## 3. Modelo

| Elemento | Camada | Papel |
|---|---|---|
| `InflationIndex` | Domain | calcula o nível de qualquer data a partir das taxas mensais e dos feriados; `Level`, `Factor`, `ToPresentValue` |
| `InflationLevel` | Domain | agregado `{Day, Level}`; nível deve ser > 0 |
| `inflation_levels` | Infrastructure | chave `id` bigint, índice único em `day` |
| `to_present_value` | Infrastructure (migration) | `to_reais(valor, dia) × nível(base) / nível(dia)`, arredondado a 8 casas |
| `RebuildInflationLevelsCommandHandler` | Application | lê IPCA e feriados pelos repositórios, calcula todos os dias, grava a diferença |

## 4. Superfície GraphQL

```graphql
mutation { rebuildInflationLevels { firstDay lastDay days levelsCreated levelsUpdated levelsRemoved } }
```

Operação de manutenção, **sem autenticação** hoje (dados de mercado são públicos até existir papel). Erro `inflation.series.invalid` quando não há IPCA importado ou a série tem mês faltando.

O valor presente em si é consumido por SQL (`SELECT to_present_value(valor, dia, base)`); não há query GraphQL ainda.

## 5. Validação

- **Domínio × SQL:** o teste de Postgres compara `to_present_value` com `InflationIndex` para quatro pares (data, base), incluindo moeda antiga, na tolerância do arredondamento.
- **Reconstrução:** idempotente (a segunda execução devolve zero criados, zero atualizados, zero removidos) e com exatamente um nível por dia corrido.
- **Carga real (2026-10-11):** 17.076 níveis, de 1980-01-15 a 2026-10-15, todos criados na primeira execução.
- **Dólar em reais constantes de out/2026:** de 1984 a 1994 a série fica em faixa estreita (R$ 10 a R$ 19) apesar de o dólar nominal ter passado de 2.881 para 1.908.120 *cruzeiros*. As quebras que sobram coincidem com **eventos reais**: Plano Cruzado (03/1986), Plano Verão (02/1989), Plano Collor (03–04/1990) e Plano Real (07–08/1994), onde o câmbio foi congelado ou redefinido por decreto.

## 6. Limites conhecidos
- Defasagem de um mês (RN-03) e ressalva de feriados 1986–1990 (RN-04).
- Sem IPCA do mês corrente: o último nível é o 15 ajustado seguinte ao último mês publicado.
- Sem gatilho automático de reconstrução (RN-07).
- `to_present_value` e `rebuildInflationLevels` são públicas.
