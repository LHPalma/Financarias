# ADR-001 — Catálogo de erros de domínio em vez de uma exceção por invariante

- **Data:** 2026-07-26
- **Status:** aceita
- **Commits:** `38a9d2e` (refactor), `749b255` (teste de catálogo)
- **Supersede:** a decisão informal anterior de "uma exceção concreta por invariante", tomada no slice de câmbio (2026-07) e reafirmada quando a generalização foi adiada

---

## Contexto

O tratamento de erro de domínio nasceu com `BaseDomainException(code, message)`, onde `Code` é a chave i18n e `message` o fallback. Cada invariante ganhava sua própria classe concreta: `InvalidCepException`, `InvalidYieldException`, `InvalidFuelPriceException`, e assim por diante.

Quando a generalização foi levantada pela primeira vez, foi **conscientemente adiada** — havia poucas exceções e a hipótese era que o tipo concreto seria útil algum dia. Com o slice de financiamento (2026-07), a contagem chegou a **13 classes**, e três medições sobre o código existente mudaram a avaliação:

| Medição | Resultado |
|---|---|
| `catch` por tipo concreto em `src/` | **0** |
| Testes que asseguravam `.Code` | **0** |
| Testes que asseguravam o tipo concreto | **27** |

O único consumidor real é o `DomainErrorFilter`, que pega `BaseDomainException` e lê `.Code`.

Daí a conclusão que motivou a mudança: os 13 tipos compravam **precisão nos testes**, e a coisa que eles representavam — o código i18n, que é o que efetivamente chega no cliente e vai chavear o `.resx` — não era assegurada em lugar nenhum. Um typo em `"analytics.monthlyrate.invalid"` passava pela suíte inteira verde e quebrava o contrato da API em silêncio.

O problema não era volume de código (13 classes somavam ~50 linhas). Era **estar travando o detalhe interno e deixando solto o contrato público.**

Além disso, 10 das 13 compartilhavam literalmente a mesma forma: código `<área>.<conceito>.invalid`, mensagem `"<Conceito> not valid: '<valor>'. <restrição>."`.

## Decisão

**Uma exceção concreta para validação, mais catálogos de código por área.**

`DomainValidationException` (`Domain/Common/Exceptions/`) é o tipo selado único para "um valor viola uma invariante". Código e mensagem vêm de um catálogo estático `<Área>Errors`, que vive na área correspondente: `AnalyticsErrors`, `AddressErrors`, `FuelErrors`, `HolidayErrors`, `LegalEntityErrors`, `MarketDataErrors`.

Cada entrada é um `public const string` com o código, mais uma factory que devolve a exceção:

```csharp
public const string MonthlyRateInvalid = "analytics.monthlyrate.invalid";

public static DomainValidationException MonthlyRate(decimal value) =>
    new(MonthlyRateInvalid, $"Monthly rate not valid: '{value}'. Must be zero or greater.");
```

O lançamento vira `throw AnalyticsErrors.MonthlyRate(value)`.

Três regras acompanham a decisão:

1. **Os testes asseguram o código como literal**, nunca via a constante do catálogo:
   ```csharp
   Assert.Equal("analytics.monthlyrate.invalid", exception.Code);
   ```
   Usar `AnalyticsErrors.MonthlyRateInvalid` compararia um símbolo consigo mesmo e deixaria o typo passar — a duplicação literal é o que trava o contrato.

2. **`DomainErrorCatalogTests`** usa reflexão sobre todos os catálogos e fixa a lista ordenada completa de códigos, sua unicidade e a convenção `<área>.<conceito>.<invariante>`. Essa lista **é** o inventário de chaves i18n; criar uma invariante quebra o teste até a chave nova ser registrada, o que é o comportamento desejado — chave de tradução deve ser adição deliberada.

3. **Tipo próprio continua certo quando a *categoria* difere**, não o campo. `UnrepresentableFinancingException` permaneceu classe própria porque significa "esta conta estoura o `decimal`", não "o usuário digitou um valor ruim". Seu código mora no `AnalyticsErrors` para o catálogo seguir completo.

Os 13 códigos foram preservados verbatim — a mudança não altera contrato.

## Alternativas consideradas

**Manter os 13 tipos e só adicionar asserção de código nos testes.** Resolveria o buraco real (o contrato não testado) com mudança mínima. Rejeitada porque preserva a cerimônia por invariante — arquivo, nome, código, mensagem — num roadmap com escrita pesada pela frente (razão dupla, transações), e mantém 13 lugares de onde enumerar chaves.

**Um catálogo único em `Domain/Common`.** Poria todas as chaves literalmente num arquivo, o que o trabalho de i18n quer. Rejeitada porque faria `Common` conhecer as mensagens de toda feature, invertendo a direção de dependência que o resto do domínio respeita. A enumeração foi resolvida por reflexão no teste, sem centralizar o código.

**Exceção genérica parametrizada (`InvalidValueException<T>`).** Rejeitada: não elimina nada que os catálogos não eliminem e piora a leitura no ponto de lançamento.

**Result/ROP em vez de exceções.** Fora de escopo desta decisão e **segue adiado**. É mudança de estilo de propagação de erro em toda a aplicação, não de organização de exceções, e mereceria ADR próprio.

## Consequências

**Positivas**
- O contrato público (`Code`) passa a ser assegurado em todos os pontos de falha.
- A lista de chaves i18n existe, é executável e não pode ficar desatualizada em silêncio.
- Criar uma invariante custa duas linhas num catálogo, não um arquivo novo.
- As mensagens de uma área ficam lado a lado, o que torna inconsistência de redação visível.

**Negativas**
- Perde-se `catch` por tipo concreto. Hoje ninguém usa, e o GraphQL devolve 200 com `errors`, então não há status HTTP para diferenciar — mas se um dia for preciso reagir diferente a uma invariante específica, será necessário voltar a promover aquele caso a tipo próprio (como já é o caso da `UnrepresentableFinancingException`).
- O teste de catálogo é deliberadamente frágil: toda chave nova o quebra. É o objetivo, mas precisa estar claro para quem esbarrar nele.
- A literal duplicada entre catálogo e teste é intencional e vai parecer redundância para quem não conhecer a razão.

**Neutras**
- 12 arquivos apagados, 14 pontos de lançamento reescritos, 27 asserções migradas; a suíte inteira (341 testes) serviu de rede.

## Relacionadas

- `docs/srs/2026-07-25-financing-price.md` — slice cujo crescimento disparou a reavaliação.
- Tradução via `.resx` + `Accept-Language` no `DomainErrorFilter` (planejada) — consumidora direta do inventário de chaves criado aqui.
