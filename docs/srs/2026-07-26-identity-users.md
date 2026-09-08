# SRS — Identidade: usuários, usuário corrente e auditoria

- **Data:** 2026-07-26
- **Feature:** `Identity/Users` + auditoria transversal (`Domain/Common`, `Infrastructure/Persistence/Interceptors`)
- **Status:** entregue (2026-09-07) — ver rastreabilidade na §8
- **Atualizado em:** 2026-09-07, para refletir o que foi realmente construído
- **Fonte externa:** nenhuma

---

## 1. Introdução

### 1.1 Propósito
Especificar a fatia de identidade do Finançarias: o agregado `User`, a porta `ICurrentUser` que informa quem está fazendo a requisição, e a auditoria automática (quando e por quem cada registro foi criado/alterado).

**Não é autenticação.** Não há senha, login, token nem sessão. O que esta fatia entrega é o *vocabulário* de identidade — o conceito de usuário e o de "usuário corrente" — para que a razão de dupla entrada, que vem em seguida, possa ser modelada com `UserId` desde o primeiro dia em vez de ser retrofitada depois.

### 1.2 Escopo desta entrega
- Agregado `User`: criar, consultar, listar, desativar, reativar.
- VO `Email`, com normalização e validação.
- Porta `ICurrentUser` (Application) + adapter **descartável** que lê a identidade de um header.
- Query `me`, que exercita a porta ponta a ponta.
- Auditoria automática de tempo (`CreatedAt`/`UpdatedAt`) e autoria (`CreatedBy`/`UpdatedBy`) via `SaveChangesInterceptor`.

**Fora de escopo, planejado para o PR seguinte (§10):** senha e hash, login, JWT, refresh token, revogação, autorização por papel.

**Fora de escopo indefinidamente nesta rodada:** o modelo de contato normalizado do Java (`UserEmail`/`UserTelephone`/`UserAddress` como coleções com tipo, status e `isPrimary`). É modelagem legítima — e reaproveitaria a integração ViaCEP, que hoje não tem consumidor além da query avulsa — mas é uma fatia inteira por si só e não destrava o ledger.

### 1.3 Definições
| Termo | Significado |
|---|---|
| **Usuário corrente** | Quem está fazendo a requisição. Nesta fatia é *declarado*, não *provado*. |
| **Porta de identidade** (`ICurrentUser`) | Contrato da Application que responde "qual o id do usuário corrente", sem dizer como isso foi determinado. |
| **Adapter descartável** | Implementação da porta feita para ser substituída: lê o id de um header, sem verificar nada. Existe para o resto do sistema poder ser escrito contra a porta. |
| **Auditoria de tempo** | `CreatedAt` / `UpdatedAt`, carimbados pela infraestrutura. |
| **Auditoria de autoria** | `CreatedBy` / `UpdatedBy` — id do usuário corrente no momento da escrita, ou nulo. |
| **Interceptor** | `SaveChangesInterceptor` do EF Core: roda antes de cada `SaveChanges` e enxerga o `ChangeTracker`. Equivalente ao `@CreationTimestamp`/`@UpdateTimestamp` do Hibernate. |
| **UUIDv7** | GUID com timestamp no prefixo, portanto ordenado no tempo. `Guid.CreateVersion7()` no .NET 9+. Diferente do v4 (`Guid.NewGuid()`), que é aleatório. |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura
```
Domain/Common
├── IHasTimestamps              (CreatedAt/UpdatedAt — sem nada de EF, como IAggregateRoot)
├── IAuditable : IHasTimestamps (+ CreatedBy/UpdatedBy)
└── Exceptions/…

Domain/Contacts
└── Email                       (VO — Create/TryCreate, normalizado)

Domain/Identity
├── User                        (agregado raiz: BaseEntity<Guid>, IAggregateRoot, IAuditable)
├── UserStatus                  (enum: Active, Inactive)
└── IdentityErrors              (catálogo de códigos — ADR-001)

Application/Common/Security
└── ICurrentUser                (porta de saída: Guid? UserId)

Application/Identity/Users
├── DTOs/Requests/CreateUserRequest
├── DTOs/Results/UserResult          (só a escrita usa — ver RN-12)
├── Commands/{CreateUser, DeactivateUser, ActivateUser}{Command, CommandHandler}
├── Queries/{IUserReads, UserReads}  (leitura Estilo B — não há query handler)
├── Specifications/UserByEmailSpecification
├── Mappers/UserMapper
└── UseCases/…

Infrastructure/Persistence
├── Configurations/UserConfiguration       (índice único no email)
├── Converters/EmailConverter
└── Interceptors/AuditableEntityInterceptor

Api
├── Security/HeaderCurrentUser             (adapter descartável — só fora de Production)
└── GraphQL/
    ├── {Query,Mutation}.cs
    └── Types/UserType                     (ObjectType<User> com BindFieldsExplicitly)
```

**Por que `Email` vai para `Domain/Contacts` e não `Domain/Identity`:** e-mail não é conceito de identidade — um posto, um emissor ou um contato de suporte também têm um. Vale a mesma regra que mandou `Cnpj` para `LegalEntities` e `Region` para `Geography`: o VO ganha a área do **conceito**, não da feature que primeiro precisou dele. `Contacts` é também para onde `Telephone` iria, se o modelo normalizado entrar depois.

**Por que a porta fica em `Application/Common/Security` e não na feature:** `ICurrentUser` não é consumida por `Identity` — é consumida por *quem precisar saber quem está agindo*, começando pelo interceptor de auditoria e, em seguida, pelo ledger. É transversal por natureza.

### 2.2 Fluxo
Escrita: `resolver` → `UseCase` (valida cru → VO, e mapeia o agregado para DTO na volta) → `CommandHandler` (lê-antes-de-escrever por `IRepository` + `Specification`, escreve) → `SaveChanges` → **interceptor carimba auditoria** → Postgres.

Leitura: **Estilo B**, não Estilo A. `resolver` → `IUserReads` → `IQueryable<User>` **aberto** → Hot Chocolate compõe `[UseProjection]/[UseFiltering]/[UseSorting]` e a seleção do GraphQL vira uma consulta SQL só. Não há UseCase nem QueryHandler na leitura, e o que sai é a **entidade curada** por `UserType`, não `UserResult` — ver RN-12.

---

## 3. Requisitos funcionais

### RF-01 — Criar usuário
`createUser(name, email)` cria um usuário `Active`. O e-mail é normalizado (RN-02) e precisa ser único (RN-01). Não há senha (RN-07).

### RF-02 — Consultar usuário por id
`user(id)` devolve o usuário, ou `null` se não existir. Inativo **é** devolvido — a consulta direta por id não esconde nada; quem esconde é a listagem (RN-03). Implementado como `IQueryable` filtrado por id fechado com `[UseFirstOrDefault]`, preservando a projeção.

### RF-03 — Listar usuários
`users` devolve os usuários **ativos**. Um argumento `includeInactive = false` permite pedir todos.

### RF-04 — Desativar usuário
`deactivateUser(id)` muda o status para `Inactive`. Desativar quem já está inativo é operação sem efeito, não erro — e **sem rastro**: como o status não muda, o EF mantém a entidade `Unchanged`, o interceptor não roda e o `UpdatedAt` fica intacto (RN-13). Id inexistente lança `identity.user.notfound`.

### RF-05 — Reativar usuário
`activateUser(id)` muda o status para `Active`. Mesma tolerância à repetição, mesma ausência de rastro (RN-13) e mesmo `identity.user.notfound` para id inexistente.

### RF-06 — Usuário corrente
`me` devolve o usuário apontado por `ICurrentUser`, ou `null` quando não há usuário corrente ou quando o id não corresponde a ninguém. É a query que prova que a porta funciona ponta a ponta — e é a única coisa que vai mudar de significado quando a autenticação de verdade entrar.

### RF-07 — Auditoria automática
Toda entidade que implementa `IAuditable` recebe, sem nenhuma chamada explícita no código de negócio:
- `CreatedAt` e `CreatedBy` no `Added`;
- `UpdatedAt` e `UpdatedBy` no `Added` **e** no `Modified`.

O carimbo é responsabilidade do `AuditableEntityInterceptor`, não do agregado nem do handler.

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | **E-mail é único.** Garantido por índice único no Postgres **e** checado por `UserByEmailSpecification` no command handler. O índice é a garantia real (a checagem tem janela de corrida); a checagem existe para devolver `identity.user.email.duplicate` em vez de um `DbUpdateException` cru. Mesma regra do `CHECK (sale_price > 0)` do combustível: invariante que importa em repouso ganha constraint também. |
| **RN-02** | **E-mail é normalizado no VO** — `Trim()` + minúsculas. Sem isso `A@x.com` e `a@x.com` driblam o índice único e viram duas contas para a mesma pessoa. A normalização é parte da identidade do valor, não formatação de exibição. |
| **RN-03** | **Inativo some da listagem, não da consulta.** `users` filtra por `Active` salvo pedido explícito; `user(id)` devolve independente do status. Desativar é esconder do uso corrente, não apagar. |
| **RN-04** | **Autoria é anulável e não tem FK.** Nula em três situações reais: o primeiro usuário do sistema não tem criador; o import da ANP roda sem requisição; migrations e seeds também. E sem FK porque é trilha de auditoria, não relacionamento — apagar ou alterar um usuário não pode travar por causa de um registro histórico. Mesmo princípio de referenciar outro agregado por id. **Nulo significa "não havia requisição"** — é informação correta, não perdida. |
| **RN-05** | **Não existe usuário de sistema.** Foi considerado semear um `User` sentinela para que `CreatedBy` nunca fosse nulo, e **rejeitado**: seria uma linha em `users` que não é pessoa, exigindo guarda na listagem, na desativação e — o custo real — no login, quando ele existir, porque seria uma conta sem senha com id fixo e conhecido. Também tentaria a reintroduzir a FK que a RN-04 removeu de propósito. Se um dia for preciso distinguir *quais* atores de sistema agiram (import, agendador, console), a modelagem certa é um `CreatedByKind` ao lado do id nulo, não linhas falsas de usuário. |
| **RN-06** | **O adapter de identidade só existe fora de Production.** Ele lê um header e acredita — quem mandar `X-User-Id: 1` *é* o usuário 1. Em Production ele não é registrado e a aplicação **falha no boot**, com mensagem explícita, em vez de subir sem identidade. Falha fechada, não aberta. Ver §7. |
| **RN-07** | **Sem senha nesta fatia.** Guardar hash sem fluxo de login é meio-trabalho, e meia-autenticação é o tipo de coisa que se faz errado. Adicionar credencial depois é migration aditiva. |
| **RN-08** | **`me` sem usuário corrente devolve `null`, não erro.** Ausência de identidade é ausência de dado, não falha — mesma convenção do câmbio (moeda ausente → `null`). Quem rejeita requisição não autenticada é a autorização, que não existe ainda. |
| **RN-09** | **Id do `User` é `Guid` v7, gerado no domínio.** `Guid.CreateVersion7()` no `User.Create()`, nunca `Guid.NewGuid()`: o v4 é aleatório e espalha a inserção pelo B-tree da PK, enquanto o v7 tem timestamp no prefixo e insere na ponta como um `int` sequencial. Gerar no domínio faz o agregado nascer completo — o handler não precisa de `SaveChanges` para saber o próprio id. O repo passa a ter tipos de id misturados (`Holiday` e `Fuel` seguem `int`): é deliberado, agregados diferentes têm necessidades diferentes. Nenhuma mudança em `BaseEntity<TId>` — `Guid` já satisfaz `struct` + `IEquatable<TId>`. |
| **RN-11** | **`email_host` é coluna gerada pelo Postgres, não coluna comum.** `GENERATED ALWAYS AS (split_part(email, '@', -1)) STORED`, mapeada como propriedade sombra (`"EmailHost"`) — o domínio não a conhece. Materializada porque relatório que agrega a tabela toda (`GROUP BY` por host) não se beneficia de índice, mas se beneficia de não chamar `split_part` por linha. **Gerada**, e não escrita pela aplicação, para não poder divergir do `email`. Usa `-1` (último campo) e não `2`, porque parte local entre aspas pode conter arroba e o VO quebra no último — com `2` o host de `"a@b"@example.com` sairia `b"`. **Domínio registrável vs. subdomínio foi rejeitado**: exige a Public Suffix List, que muda com o tempo, então a coluna viraria um retrato desatualizado. |
| **RN-12** | **Leitura devolve a entidade curada; escrita devolve DTO.** `user`/`users`/`me` expõem `User` através de `UserType : ObjectType<User>` com `BindFieldsExplicitly()`; `createUser`/`deactivateUser`/`activateUser` devolvem `UserResult`. A assimetria é deliberada: leitura Estilo B só se paga se o `IQueryable` chegar aberto no Hot Chocolate, e um DTO no meio mataria a projeção. `CreatedBy`/`UpdatedBy` ficam fora dos dois. Consequência aceita: o schema tem dois tipos para o mesmo conceito, e um rename no agregado tem efeito imediato no contrato se o `ObjectType` não for mantido em dia. |
| **RN-13** | **Repetição não deixa rastro.** `Deactivate()`/`Activate()` em quem já está no estado alvo não geram UPDATE nem mexem no `UpdatedAt`. Isso depende de o handler salvar com `SaveChangesAsync`, **não** com o `UpdateAsync` do Ardalis — o `Update` marca todas as propriedades como modificadas e faria a repetição carimbar. Não há early return no handler: o `SELECT` do `GetByIdAsync` é inevitável (é preciso carregar para saber o status) e o `SaveChanges` sem mudanças já retorna antes de abrir conexão, então a guarda economizaria zero ida ao banco e tiraria do agregado a decisão sobre o próprio estado. |
| **RN-10** | **O agregado não sabe que é auditado.** `User` expõe `CreatedAt`/`UpdatedAt`/`CreatedBy`/`UpdatedBy` com `private set` e nunca os atribui. O interceptor escreve via `entry.Property(...).CurrentValue`, que atravessa o setter privado sem o domínio afrouxar encapsulamento. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Auditoria não esquecível):** o carimbo mora num interceptor, não numa chamada em cada handler. Não existe caminho de escrita que "esqueça" de auditar — que é exatamente o modo de falha da abordagem manual.
- **RNF-02 (Tempo injetado):** o interceptor recebe `TimeProvider`, não chama `DateTimeOffset.UtcNow`. Em teste, um `TimeProvider` fake com hora fixa torna a asserção exata; sem isso, verificar "o `UpdatedAt` mudou" viraria `Thread.Sleep` ou tolerância.
- **RNF-03 (Domínio puro):** `IHasTimestamps` e `IAuditable` vivem em `Domain/Common` sem referência a EF — exatamente como `IAggregateRoot`. O mecanismo é 100% Infrastructure. **Divergência do que esta seção dizia antes:** o contrato foi dividido em dois em vez de um só com quatro membros, para que tabela escrita por import ou job possa carimbar tempo sem ganhar coluna de autoria que seria nula para sempre. Também não são marcadoras vazias: declaram as propriedades como `{ get; }`, o que dá checagem em tempo de compilação sem afrouxar nada.
- **RNF-04 (Porta antes do mecanismo):** nada além do adapter conhece *como* a identidade chega. Quando o JWT entrar, muda um arquivo em `Api/Security`; domínio, use cases e o ledger futuro ficam intactos.
- **RNF-05 (Sem timestamps retroativos):** as entidades existentes (`Holiday`, `FuelStation`, `FuelPrice`) **não** passam a ser auditáveis nesta fatia — seria migration em tabela grande sem demanda. `User` é a primeira e única. Quando a primeira delas entrar, entra por `IHasTimestamps`, sem colunas de autoria (RNF-03).

---

## 6. Interface — GraphQL

```graphql
type Query {
  user(id: UUID!): User
  users(includeInactive: Boolean! = false): [User!]!
  me: User
}

type Mutation {
  createUser(input: CreateUserRequestInput!): UserResult!
  deactivateUser(id: UUID!): UserResult!
  activateUser(id: UUID!): UserResult!
}

input CreateUserRequestInput {
  name: String!
  email: String!
}

enum UserStatus { ACTIVE INACTIVE }

type User {
  id: UUID!
  name: String!
  email: String!
  status: UserStatus!
  createdAt: DateTime!
  updatedAt: DateTime!
}

type UserResult {
  id: UUID!
  name: String!
  email: String!
  status: UserStatus!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

`User` é a entidade curada por `UserType`, saída das **queries**; `UserResult` é o DTO, saída das **mutations** (RN-12). `CreatedBy`/`UpdatedBy` não são expostos em nenhum dos dois: são trilha interna, e expor "quem alterou" é decisão de produto que ainda não foi tomada — mais fácil adicionar campo depois do que remover.

**Três coisas divergem do que esta seção especificava antes**, todas consequência de a leitura ter ido para o Estilo B (§2.2, RN-12):

1. As queries devolvem `User`, não `UserResult`. O DTO ficou só na escrita.
2. O input chama-se `CreateUserRequestInput`, não `CreateUserInput`: o record em `DTOs/Requests` segue a convenção `*Request` do repo (como `SimulateFinancingRequest`) e o Hot Chocolate acrescenta o sufixo.
3. `users` mantém o argumento `includeInactive` em vez de deixar o cliente filtrar por `where`. "Ativo por padrão" é regra de negócio (RN-03), não preferência de quem consulta.

**O campo `email` é achatado para `String!`**, e isso exige um detalhe que não é óbvio: no `UserType` o campo precisa ser declarado **a partir do membro** — `descriptor.Field(u => u.Email)` e só então `.Name("email")`, `.Type<NonNullType<StringType>>()` e `.Resolve(...)`. Com `descriptor.Field("email")` mais um resolver solto, a projeção não sabe que precisa da coluna, o `Parent<User>().Email` chega nulo e o cliente recebe `"Unexpected Execution Error"`. Não quebra o build nem teste unitário — só teste funcional com banco real pega.

**Erros de domínio chegam como `extensions.code`** pelo `DomainErrorFilter`: `contacts.email.invalid`, `identity.user.name.required`, `identity.user.email.duplicate` e `identity.user.notfound`.

---

## 7. Segurança — o buraco é consciente e precisa de trava

O `HeaderCurrentUser` lê `X-User-Id` e acredita. Isso é **bypass total de autenticação** — não é uma fraqueza, é a ausência da coisa. O que impede isso de virar incidente é a trava de RN-06:

- O adapter é registrado **apenas** quando `!Environment.IsProduction()`.
- Em Production, nada implementa `ICurrentUser`. Como o `AuditableEntityInterceptor` depende da porta e é um serviço registrado, o `ValidateOnBuild` que o `Program.cs` já liga faz a aplicação **falhar no boot**.
- Para o erro não ser um `InvalidOperationException` críptico de DI, o `Program.cs` lança explicitamente em Production com mensagem dizendo que a autenticação real não foi implementada.

O efeito é que este projeto **não sobe em Production** enquanto não houver autenticação de verdade — o que é a resposta correta, e não um bloqueio a contornar. Enquanto a escolha for "sem auth", o certo é não ter ambiente exposto.

---

## 8. Rastreabilidade (requisito → commit)

Entregue em `feat/identity-users` e mergeada em `main` em 2026-09-07 (PR #28, merge commit `0500a15`, sem squash nem rebase justamente para os hashes abaixo continuarem válidos).

| Commit | Entrega | Requisitos |
|---|---|---|
| `710265b` | `ICurrentUser` + `HeaderCurrentUser` + trava de boot | RN-06, RNF-04, §7 |
| `24e32c2` | `IHasTimestamps`/`IAuditable` + `AuditableEntityInterceptor` + `TimeProvider` | RF-07, RN-10, RNF-01/02/03 |
| `903d836` | VO `Email` + `ContactsErrors` | RN-02 |
| `9c151de` | catálogo de erros comparado por conjunto, não por ordem | — (dívida de teste) |
| `a3a7efd` | `Email.Host` / `Email.LocalPart` | RN-11 |
| `dc0badb` | agregado `User` + `UserStatus` + `IdentityErrors` | RF-01/04/05, RN-09, RN-10 |
| `ccec1c6` | mapeamento EF + `EmailConverter` + índice único + `email_host` + migration | RN-01, RN-11 |
| `64c824f` | `email_host` passa a quebrar no último arroba | RN-11 |
| `19d810a` | `CreateUser` command/handler/use case + specification | RF-01, RN-01 |
| `0c6ff56` | `user`, `users`, `me` como leitura composta | RF-02/03/06, RN-03, RN-08, RN-12 |
| `ff7e86d` | mutation `createUser` | RF-01 |
| `485b9f7` | mutation `deactivateUser` | RF-04, RN-13 |
| `30fe8df` | mutation `activateUser` | RF-05, RN-13 |

---

## 9. Cobertura de testes

366 testes verdes na suíte inteira ao fim da fatia.

| Nível | Cobre |
|---|---|
| `EmailTests` | normalização (trim, minúsculas); inválidos lançam com o código certo; `TryCreate` nos dois caminhos; igualdade por valor; `Host`/`LocalPart` quebrando no **último** arroba |
| `UserTests` | `Create` nasce `Active`, com nome aparado e id **v7**; nome em branco lança com o código; desativar/reativar; repetição é no-op; auditoria **não** é escrita pelo agregado |
| `DomainErrorCatalogTests` | inventário de chaves i18n comparado por **conjunto**, com a falha nomeando o código não registrado |
| `HeaderCurrentUserTests` | header válido, ausente, não-GUID e **sem `HttpContext`** (import/job) |
| `AuditableEntityInterceptorTests` (Testcontainers) | `Added` carimba os quatro campos; `Modified` mexe só nos `Updated*`; sem usuário corrente grava autoria nula; relógio falso torna a asserção exata |
| `UserPersistenceTests` (Testcontainers) | round-trip do `EmailConverter`; índice único rejeita duplicata **normalizada**; `email_host` calculada pelo Postgres nos dois formatos de arroba |
| `CreateUserHandlerTests` (Testcontainers) | o agregado devolvido já vem carimbado — sem isso o `UserResult` sairia com `CreatedAt` no default |
| `DeactivateUserHandlerTests` (Testcontainers) | desativar duas vezes com três horas de intervalo deixa o `UpdatedAt` na primeira (RN-13) |
| `CreateUserCommandHandlerTests` | e-mail duplicado lança o código **antes** de tentar escrever |
| `{Activate,Deactivate}UserCommandHandlerTests` | muda o status, tolera repetição, e id inexistente lança `identity.user.notfound` sem salvar |
| `CreateUserUseCaseTests` | monta o comando com o VO normalizado, mapeia para `UserResult`, e e-mail inválido lança antes de chamar o handler |
| `UserQueriesTests` (funcional) | `email` achatado sob projeção; `users` esconde inativo por padrão e mostra com `includeInactive`; `me` com e sem header |
| `UserMutationsTests` (funcional) | `createUser` cria e aparece em `users`; ciclo desativa → some → reativa → volta; os três códigos de erro em `extensions.code` |

---

## 10. Fatia seguinte — autenticação de verdade

Especificada em `docs/srs/2026-09-07-auth-jwt.md`. Em resumo: senha com Argon2id (`Isopoh.Cryptography.Argon2`), `login` devolvendo JWT de acesso, `JwtCurrentUser` no lugar do `HeaderCurrentUser`, `[Authorize]` nos resolvers e remoção da trava de boot da RN-06. Refresh token e revogação ficam para a fatia depois dessa.

**O que aquela fatia cobra desta:** a promessa da RNF-04 de que trocar o adapter é *um arquivo*. Os únicos consumidores de `ICurrentUser` entregues aqui — o interceptor de auditoria e o resolver `me` — recebem apenas `Guid?`, então a troca não deve tocar domínio, casos de uso nem handlers. Se tocar, o problema está aqui, não lá.

---

## 11. Itens em aberto

- **Contato normalizado** (`UserEmail`/`UserTelephone`/`UserAddress`) — daria consumidor real para a integração ViaCEP, hoje órfã. O e-mail hoje é **uma coluna** em `users`, não tabela à parte; normalizar depois é migration aditiva e não toca o VO.
- **Tornar as entidades existentes auditáveis** — `FuelPrice` seria o caso com valor. Entraria por `IHasTimestamps`, sem colunas de autoria (RNF-05).
- **Expor autoria no GraphQL** — decisão de produto, não técnica.
- **Papéis e autorização** — fatia própria, depois desta. Provavelmente merece ADR (papel no usuário vs. permissão por recurso). É onde um contexto de usuário mais rico, montado a partir das claims do token, passa a se pagar — ao lado de `ICurrentUser`, não no lugar dele.
- **ADR "identidade própria em vez de ASP.NET Core Identity"** — decisão transversal com alternativa séria, ainda não escrita.
- **`identity.user.notfound` como `DomainValidationException`** — se "não encontrado" virar categoria recorrente, merece tipo próprio, como `UnrepresentableFinancingException` tem.
- **Documentação do `IFuelReads`** — o `IUserReads` ganhou um resumo que explica o contrato e o modo de falha de materializar; o `IFuelReads`, que tem o mesmo papel, não.
- **`PostgreSqlBuilder()` sem parâmetro está obsoleto** (CS0618) em 9 arquivos de teste. Não quebra hoje; quebra quando o Testcontainers remover.
