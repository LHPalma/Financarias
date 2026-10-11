# SRS — Cadastro de emissores e classificação setorial

- **Data:** 2026-10-11
- **Feature:** `Domain/MarketData/Issuers` + `Application/MarketData/Issuers` + `Integrations/MarketData/B3/Issuers`
- **Status:** **entregue**, exceto a query de leitura. Agregado, tabela `issuers`, importação e a mutation `importIssuers` estão em `main`, com carga real (§5).
- **Fonte:** dois endpoints do proxy de empresas listadas do site da B3 (`sistemaswebb3-listados.b3.com.br/listedCompaniesProxy/CompanyCall/`), os mesmos do `0010`: `GetInitialCompanies` (lista paginada) e `GetDetail` (por código CVM). Sem chave e sem contrato publicado.

---

## 1. Introdução

### 1.1 Propósito
Saber de que setor é cada empresa. Os estudos de evento escolhiam papéis à mão (BBAS3, PETR4, ITUB4…); com o setor, a pergunta passa a ser "o que aconteceu com os bancos?" ou "com a mineração?", sobre todos os emissores de uma vez.

### 1.2 Escopo
Um cadastro por **emissor** (a raiz de quatro caracteres do ISIN: `PETR`, `VALE`), com razão social, nome de pregão, CNPJ, código CVM, mercado de listagem, atividade, site e a classificação setorial em três níveis.

**Fora de escopo:** histórico de classificação (a B3 só informa a de hoje), código CVM e CNPJ de quem saiu da bolsa, ações em circulação, administradores, e a query de leitura.

## 2. Regras

### RN-01 — O conjunto tentado sai das cotações
A importação não cadastra as 3.541 empresas que a B3 lista: cadastra os emissores que **têm cotações já importadas** (prefixo `BR` + 4 caracteres do ISIN, 3.298 hoje) e que a B3 lista. O resto não interessa ao warehouse e custaria uma chamada de detalhe cada.

### RN-02 — Emissor que a B3 não lista fica sem cadastro
`GetInitialCompanies` devolve só empresas ativas. Quem saiu da bolsa (Aracruz, Sadia…) e tudo que o prefixo `BR` pega e não é emissor listado (BDRs, ETFs, fundos) conta em `notListed` e não ganha linha. É o mesmo limite do `0010`, §2.2.

### RN-03 — Classificação em três níveis
A B3 devolve `industryClassification` como `Setor / Subsetor / Segmento`. O provedor separa por `" / "`: `Petróleo. Gás e Biocombustíveis / Petróleo. Gás e Biocombustíveis / Exploração. Refino e Distribuição` vira setor, subsetor e segmento. O texto é gravado como a B3 escreve, inclusive com pontos no lugar de vírgulas.

### RN-04 — "Não Classificados" é um valor, não ausência
301 dos 679 cadastros caíram em `Não Classificados` (holdings sem atividade classificada, fundos de investimento em participações, emissores de dívida). Quem agrupar por setor para um relatório de ações deve filtrar esse valor.

### RN-05 — Mercado de listagem vem do detalhe, não da lista
A lista traz `segment` (`Não Classificados` para a maioria), enquanto o detalhe traz `market` (`NOVO MERCADO`, `BOVESPA NIVEL 1`, `BOVESPA NIVEL 2`, `BOLSA`, `BDR / CDA`). Vale o do detalhe, gravado em `listing_market`.

### RN-06 — Detalhe que não responde não impede o cadastro
Se `GetDetail` responde 404 ou corpo não-JSON, o emissor é cadastrado com os dados da lista e sem classificação (setor nulo). Uma importação seguinte completa quando a B3 voltar a responder.

### RN-07 — Atualiza, não duplica nem remove
O código do emissor tem índice único. A importação cria o que falta, atualiza o que a B3 mudou (`Issuer.Update` devolve se algo mudou, e só então há escrita) e **não remove** quem sumiu da lista: um emissor que sai da bolsa mantém o último cadastro conhecido.

### RN-08 — CNPJ inválido vira nulo
O VO `Cnpj` valida só os 14 dígitos. Um CNPJ que a B3 devolve vazio ou malformado fica nulo, e não derruba a importação.

### RN-09 — A importação é pesada
Uma chamada de detalhe por emissor listado, com paralelismo 4. A carga inteira (679 emissores) leva alguns minutos, dentro do limite de 10 minutos da execução GraphQL. Primeira tentativa dessa carga falhou com um erro transitório de conexão ao banco na leitura dos códigos (varredura de 7 milhões de cotações); repetir resolveu.

## 3. Modelo
`issuers`: `id`, `issuer_code` (único, 4), `cvm_code`, `company_name`, `trading_name`, `cnpj` (via `CnpjConverter`), `sector`, `subsector`, `segment`, `listing_market`, `activity` (texto), `website`. Invariante no domínio: código de emissor válido e nomes e código CVM não vazios (`issuer.company.invalid`).

## 4. Superfície GraphQL
`importIssuers`: mutation sem argumento que devolve `{ issuersWithQuotes, notListed, created, updated }`. Manutenção, sem autenticação hoje.

## 5. Carga e validação (2026-10-11)
3.298 emissores com cotação, 2.619 não listados, **679 cadastrados**, 311 com CNPJ, 659 com setor. Os papéis do relatório de eleições saem corretos: BBAS e ITUB em *Financeiro / Intermediários Financeiros / Bancos*, BBSE em *Previdência e Seguros*, ITSA em *Holdings Diversificadas*, PETR em *Petróleo. Gás e Biocombustíveis*, VALE em *Materiais Básicos / Mineração*. Maiores setores: Consumo Cíclico (85), Financeiro (56), Utilidade Pública (52), Bens Industriais (49), Materiais Básicos (30).

Testes: domínio (invariantes, `Update` com e sem mudança), handler (cria, atualiza, não toca, CNPJ inválido), caso de uso, provedor com cliente simulado (paginação, classificação separada, detalhe ausente), persistência em Postgres (unicidade, `IssuerCodesAsync`, ciclo criar-atualizar-repetir), funcional da mutation e um teste de contrato contra a B3 real.

## 6. Limites conhecidos
- Empresas extintas não têm cadastro (RN-02), então um estudo de setor sobre 1994–2010 perde quem saiu. O setor delas só dá para inferir à mão.
- Classificação de hoje aplicada a todo o passado: uma empresa que mudou de setor aparece no atual.
- Sem query de leitura GraphQL; o uso hoje é por SQL (`issuers` junto de `daily_quotes` pelo prefixo do ISIN).
