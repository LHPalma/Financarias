# SRS — Indicadores do mercado externo (petróleo, minério de ferro, índice do dólar, VIX)

- **Data:** 2026-10-11
- **Feature:** `Domain/MarketData/Indicators` + `Integrations/MarketData/{Eia,Cboe,Fed,WorldBank}` + `IndicatorValueProviderRouter`
- **Status:** **entregue.** Cinco indicadores novos no mesmo agregado e na mesma mutation do `0009`, com carga real (§5). Falta a query de leitura, como no `0009`.
- **Fontes:** quatro publicações públicas, **sem chave**, e sem contrato publicado além do arquivo ou do CSV que servem.

---

## 1. Introdução

### 1.1 Propósito
Dar contexto externo aos estudos de evento: separar o que é política brasileira do que é petróleo, minério ou aversão global a risco. A Petrobras e a Vale seguem o Brent e o minério; o índice do dólar e o VIX dizem se o dia foi ruim no mundo inteiro.

### 1.2 Escopo
| Indicador (`EconomicIndicator`) | Série | Frequência | Início | Fonte |
|---|---|---|---|---|
| `BrentOil` | Brent à vista (Europa), US$/barril | diária | 20/05/1987 | EIA, arquivo `RBRTEd.xls` |
| `WtiOil` | WTI à vista (Cushing), US$/barril | diária | 02/01/1986 | EIA, arquivo `RWTCd.xls` |
| `IronOre` | Minério de ferro 62% Fe CFR China, US$/t | mensal (dia 1) | jan/1960 | Banco Mundial, "Pink Sheet", coluna `Iron ore, cfr spot` |
| `DollarIndex` | Nominal Broad Dollar Index (`JRXWTFB_N.B`) | diária | 02/01/2006 | Federal Reserve, relatório H.10 |
| `Vix` | VIX, fechamento | diária | 02/01/1990 | Cboe, `VIX_History.csv` |

**Fora de escopo:** o DXY da ICE (pago; o índice do Fed pesa mais moedas e anda junto), futuros, preço intradiário, outras commodities do Pink Sheet (a planilha traz dezenas, e a leitura por cabeçalho já permitiria acrescentar).

## 2. Decisões

### RN-01 — Mesmo agregado, mesma mutation
Os valores entram em `indicator_values` e se importam por `importIndicatorValues(input: { indicator, year })`, como o dólar e o IPCA. Um indicador novo é um valor de enum e uma linha no roteador.

### RN-02 — Roteador por indicador
`IIndicatorValueProvider` continua uma porta só. `IndicatorValueProviderRouter` é a implementação registrada e delega ao provedor do indicador (Banco Central, EIA, Cboe, Fed, Banco Mundial). O handler de importação não mudou.

### RN-03 — Por que não a FRED
A primeira implementação usava a FRED (uma fonte só, cinco séries). Dois problemas a derrubaram: o endpoint de CSV público **recusa clientes .NET** (a conexão é reiniciada para qualquer `User-Agent` que não seja de ferramenta conhecida, e fingir um não é uma opção), e a API oficial exige uma chave. Trocar por quatro fontes sem chave custou mais código e deixou a carga independente de cadastro. O histórico de git guarda a tentativa.

### RN-04 — O endereço do minério muda a cada edição
O Banco Mundial publica a planilha em `thedocs.worldbank.org/en/doc/<código da edição>/related/CMO-Historical-Data-Monthly.xlsx`, e o código muda a cada atualização. O provedor lê a página `worldbank.org/en/research/commodity-markets`, extrai o código do link e só então baixa a planilha. Se a página deixar de ter o link, devolve lista vazia e registra aviso.

### RN-05 — Resposta vazia do Fed não é "ano sem dados"
O endpoint do Fed, de forma intermitente, devolve status 200 com corpo vazio. Lido ingenuamente, vira "ano sem dados" e **a importação parece ter dado certo** (foi o que aconteceu na primeira carga: 14 dos 21 anos vieram vazios). O provedor exige o cabeçalho da série (`Time Period`), tenta até quatro vezes com pausa e, se persistir, lança erro.

### RN-06 — Precisão
O valor é arredondado a 6 casas antes de comparar e gravar (`numeric(18,6)`). Sem isso, séries com muitas casas, como o minério, apareceriam como "atualizadas" a cada importação.

### RN-07 — Dia sem observação não vira linha
Feriado americano não tem Brent, WTI, VIX nem índice. O provedor pula, não repete o valor anterior. Quem precisar de uma série contínua preenche na leitura (o relatório de eleições já faz isso por papel).

## 3. Dados conhecidos
- **O WTI foi negativo em 20/04/2020 (US$ -36,98).** É dado real, não erro, e o `numeric` aceita.
- O Pink Sheet só informa o mês: o minério fica no dia 1, como o IPCA.
- Brent e WTI vêm de uma planilha que a EIA recalcula; um valor revisado entra como `valuesUpdated` na próxima importação.

## 4. Superfície GraphQL
`importIndicatorValues` ganhou cinco valores de enum (`BRENT_OIL`, `WTI_OIL`, `IRON_ORE`, `DOLLAR_INDEX`, `VIX`), cada um documentado no schema com unidade, início e fonte. Operação de manutenção, sem autenticação hoje. Cada chamada baixa o arquivo inteiro da fonte para devolver um ano; a carga completa de um indicador leva alguns minutos.

## 5. Carga e validação (2026-10-11)
| Indicador | Linhas | De | Até | Mín. | Máx. |
|---|---|---|---|---|---|
| `BrentOil` | 9.102 | 20/05/1987 | 06/10/2026 | 9,10 | 143,95 |
| `WtiOil` | 9.517 | 02/01/1986 | 06/10/2026 | -36,98 | 145,31 |
| `Vix` | 9.291 | 02/01/1990 | 09/10/2026 | 9,14 | 82,69 |
| `DollarIndex` | 5.203 | 02/01/2006 | 02/10/2026 | 85,47 | 130,04 |
| `IronOre` | 561 | jan/1980 | set/2026 | 24,30 | 214,40 |

Testes: unidade por provedor (planilhas montadas com ClosedXML, CSV literal), roteamento de todos os valores do enum, e um teste de contrato por indicador contra as fontes reais (`Integrations.IntegrationTests`, rodado semanalmente fora do CI principal), que confere, entre outras coisas, o piso do Brent em 21/04/2020 (US$ 9,12).

## 6. Limites conhecidos
- Sem query de leitura GraphQL.
- Quatro fontes sem contrato: qualquer uma pode mudar de formato. O teste de contrato semanal é o aviso.
- O índice do dólar do Fed tem rótulo de base desatualizado no próprio arquivo (diz janeiro de 1997 = 100; os valores batem com a base de 2006 da série equivalente da FRED).
