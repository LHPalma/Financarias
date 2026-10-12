# SRS — Composição histórica dos índices da B3

- **Data:** 2026-10-12
- **Feature:** `Domain/MarketData/IndexComposition` + `Application/MarketData/IndexComposition` + `Integrations/MarketData/B3/IndexComposition`
- **Status:** **entregue**, exceto a query de leitura e a descoberta automática dos arquivos. Entidade, tabela `index_composition_items`, leitor de planilhas, cliente Refit e a mutation `importIndexComposition` estão no working tree, com carga real (§5).
- **Fonte externa:** arquivos de carteira (planilha ou ZIP) anexados a comunicados da B3, em `https://www.b3.com.br/data/files/...`. Sem chave e sem contrato: o formato da planilha é de fato, não prometido.

---

## 1. Introdução

### 1.1 Propósito
Guardar, no tempo, **quais ações compõem cada índice e com que quantidade teórica**. Nasceu da pergunta "a queda do Ibovespa é causada pela Vale?" (relatório de eventos): responder exige o peso da ação no índice **naquele dia**, e a B3 só publica a composição vigente.

### 1.2 Por que quantidade, e não só o peso
O Ibovespa é `Σ(quantidade teórica × preço) / redutor`. A quantidade é fixa dentro de um quadrimestre (jan, mai, set), então guardar **quantidade e redutor** permite recalcular o peso exato de qualquer ação em qualquer pregão com o preço do dia. O peso publicado vale só no pregão-base da carteira e deriva 1 a 4 % em um quadrimestre, porque o índice é de retorno total.

### 1.3 Escopo
Dentro: ler planilhas de carteira (todos os índices que a planilha traz, 38 códigos), importar e repetir sem duplicar. Fora: a query de leitura, a composição vigente (já exposta pela B3 em endpoint próprio), a descoberta automática dos arquivos e qualquer período anterior a 2017 (§6).

## 2. Regras

### RN-01 — Uma linha por índice, quadrimestre, tipo de carteira e ação
Chave natural única: `(índice, ano, quadrimestre, tipo, ticker)`. O tipo é `FirstPreview`, `SecondPreview`, `ThirdPreview` ou `Final` (a "virada"). Prévias e final ficam separadas porque a composição muda entre elas e é isso que se quer estudar.

### RN-02 — O período e o tipo vêm do título da aba, nunca do nome do arquivo
O nome do arquivo e a data da captura enganam: páginas antigas apontam para anexos de outro quadrimestre. O título da primeira linha da aba (`3ª Prévia do IBOVESPA para Janeiro a Abril de 2020`, `VIRADA do IBOVESPA para Janeiro de 2022`) é a única fonte confiável. O quadrimestre é derivado do primeiro mês citado. Aba cujo título não segue o padrão não é carteira e é ignorada.

### RN-03 — O índice é o nome da aba
A aba `IBxl` é o IBrX 50; o título dela diz `IBrX 50` e o da aba `IGC` diz `IGCX`. O nome da aba é estável e é gravado em maiúsculas.

### RN-04 — Planilha de entradas e saídas não é carteira
Os ZIPs trazem junto uma planilha de resumo de entradas e saídas, sem quantidade. Ela é lida sem erro e conta zero carteiras. Entradas `__MACOSX/._*` dos ZIPs feitos em Mac são ignoradas.

### RN-05 — Formatos tolerados
- Cabeçalho `COD./ACAO/TIPO/QTDE. TEORICA/PART. %` ou a variante acentuada (`CÓDIGO/AÇÃO/QTDE. TEÓRICA`), localizado por nome de coluna e não por posição.
- Números como número ou como texto, com ponto ou vírgula decimal e separador de milhar.
- Linha de total (`QUANTIDADE TEORICA TOTAL`) e linhas sem ticker válido são descartadas.
- `VALOR DO REDUTOR` e `PREGÃO BASE: dd/mm/aaaa` são lidos do rodapé. Quando ausentes (as de 2024) ficam nulos; data impossível também fica nula, sem derrubar o arquivo.

### RN-06 — Precisão gravada igual à lida
Peso com 4 casas e redutor com 8, as casas da coluna. O domínio arredonda antes de comparar, senão uma reimportação veria todo valor como alterado.

### RN-07 — Só arquivos de dados da B3
A mutation recusa qualquer endereço que não seja `https://www.b3.com.br/data/files/...` (`indexcomposition.source.invalid`), e a validação é feita antes de qualquer download. A mutation é pública e baixa o que lhe mandam, então o host fixo evita usá-la para fazer a API requisitar endereço arbitrário.

### RN-08 — Arquivo com falha não derruba o lote
Cada arquivo é importado por conta própria; o que falha entra em `failures` com o motivo, o rastreamento do contexto é limpo e os demais seguem.

### RN-09 — Importação idempotente
Repetir atualiza em vez de duplicar. O endereço de origem faz parte do que se compara: o mesmo conteúdo vindo de outro arquivo conta como atualização (4 039 das 134 276 linhas da carga inicial, que existem em mais de um anexo).

## 3. Superfície GraphQL

`importIndexComposition(input: { urls: [String!]! }): IndexCompositionImportResult!` com `filesRead`, `portfolios`, `rows`, `created`, `updated` e `failures { url reason }`. Pública, como as demais importações (ver `docs/debt/0008-public-maintenance-mutations.md`).

## 4. Arquitetura

Entidade achatada `IndexCompositionItem` (uma linha por ativo), em vez de carteira com itens: pregão-base e redutor repetem em cada linha, e em troca o upsert é o mesmo das outras importações, sem agregado com filhos. Leitor `IndexCompositionWorkbookReader` (ExcelDataReader, `.xls` e `.xlsx`, planilha direta ou ZIP), cliente Refit `IB3DataFileClient` com `GET /data/files/{**path}`, provedor `B3IndexCompositionProvider`, caso de uso que valida os endereços e agrega os arquivos.

## 5. Carga e validação

Carga de 99 anexos achados nas páginas de comunicados da B3: 134 274 linhas, 38 índices, Ibovespa de **2017 Q3 a 2025 Q1** (com lacunas, §6). A validação é a identidade do §1.2 contra os preços do COTAHIST e o fechamento oficial no pregão-base de cada carteira:

- Maioria dos casos entre 0,000 % e 0,13 % de diferença (a diferença residual é o arredondamento do redutor e ações sem pregão no dia).
- **Caso fora do padrão:** `2020 Q2 Final` (base 30/04/2020), +1,1 %. Não investigado; suspeita de que a carteira final use uma data-base diferente da que o rodapé declara.

## 6. Lacunas conhecidas

- **Antes de 2017 Q3** só existem PDFs de entradas e saídas, sem quantidade. Reconstruir exigiria partir de uma carteira conhecida e aplicar as mudanças, e não foi feito.
- **Depois de 2025 Q1** o que há é a composição vigente da B3 (endpoint já mapeado, não importado).
- **Quadrimestres com tipo faltando** (ex.: 2018 Q1 sem prévia 1 e final, 2020 Q2 só final): os anexos correspondentes não foram achados nas páginas coletadas.
- **A lista de arquivos é estática**, levantada por raspagem das páginas de comunicados e do Wayback Machine. Não há descoberta automática; publicar uma carteira nova exige passar o endereço à mutation.
