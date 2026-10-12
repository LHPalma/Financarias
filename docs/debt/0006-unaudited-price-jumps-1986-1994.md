# TDR-006 — Preços de 1986 a 1994 sem auditoria de saltos, e nenhuma série ajustada por eventos

- **Data:** 2026-10-11
- **Status:** aberto (dívida conhecida; é o próximo passo natural do armazém de cotações)
- **Commits:** `8ae2541` (SRS 0007 com a conferência da conversão), `7c6e424` (SRS 0010 com o import de eventos)
- **Quadrante (Fowler):** deliberado + prudente — os eventos foram importados primeiro porque a série ajustada depende deles, e o aviso está escrito onde o dado é descrito

---

## Contexto

O COTAHIST guarda o preço **como publicado**, com o fator de cotação ao lado (`quote_factor`: 1, 100 ou 1.000 ações por preço). Os eventos corporativos (`0010`) guardam desdobramentos, grupamentos, bonificações e proventos como publicados. A conversão de moedas (`0018`) leva cada preço a reais. Falta combinar as três coisas numa **série de preço ajustada**.

## Débito assumido

1. **Não existe série ajustada nem retorno total.** Um desdobramento aparece como uma queda de preço, e a data ex de um provento distorce o dia (o caso da PETR4 em 12/08/2022).
2. **Os preços de 1986 a 1994 não passaram por auditoria de saltos.** Em oito papéis líquidos, 20 variações diárias foram maiores que 2,5× ou menores que 0,4× depois da conversão de moeda, sem desdobramento, grupamento ou bonificação correspondente. Alguns são de cerca de 1.000× e caem no mesmo dia em papéis diferentes (BBDC4 e ITAU4 em 01/06/1987), o que aponta para mudança de lote de cotação sem o `quote_factor` acompanhar. Os demais apontam para eventos que a B3 não publica para o período.

## Impacto

- **Retornos de 1986 a 1994 não são confiáveis** sem tratamento: um salto de 1.000× entra como retorno de 99.900%.
- Retornos recentes têm o erro menor dos proventos: o relatório de eleições avisa que não ajusta por dividendo e JCP.
- Estudos em escala (setor, mercado inteiro) herdam esses saltos se não filtrarem.

## Alternativa correta (não implementada)

Um slice próprio de **preço ajustado**: (a) normalizar o preço por ação (`preço ÷ quote_factor`); (b) aplicar os fatores dos eventos conforme o tipo (percentual de aumento para desdobramento e bonificação, multiplicador para grupamento), depois de validar a semântica do fator em escala, porque ela só foi conferida num emissor; (c) rodar a **auditoria de saltos** sobre o resultado, sinalizando variações sem evento correspondente, para separar o que falta em `corporate_events` do que é erro de dado; (d) calcular o retorno total reinvestindo os proventos pela data ex.

## Quando resolver

Antes de qualquer estudo de retorno que cruze 1986–1994, ou que dependa de dividendos. Para 1994 em diante, no nível de um evento isolado, o aviso do relatório basta.
