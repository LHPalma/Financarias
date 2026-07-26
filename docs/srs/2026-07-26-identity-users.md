# SRS — Identidade: usuários, usuário corrente e auditoria

- **Data:** 2026-07-26
- **Feature:** `Identity/Users` + auditoria transversal (`Domain/Common`, `Infrastructure/Persistence/Interceptors`)
- **Status:** especificado — implementação não iniciada
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
├── IAuditable                  (interface marcadora — sem nada de EF, como IAggregateRoot)
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
├── DTOs/{Requests, Results}
├── Commands/{CreateUser, DeactivateUser, ActivateUser}{Command, CommandHandler}
├── Queries/{GetUserById, ListUsers, GetCurrentUser}{Query, QueryHandler}
├── Specifications/UserByEmailSpecification
└── UseCases/…

Infrastructure/Persistence
├── Configurations/UserConfiguration       (índice único no email)
├── Converters/EmailConverter
└── Interceptors/AuditableEntityInterceptor

Api
├── Security/HeaderCurrentUser             (adapter descartável — só fora de Production)
└── GraphQL/{Query,Mutation}.cs
```

**Por que `Email` vai para `Domain/Contacts` e não `Domain/Identity`:** e-mail não é conceito de identidade — um posto, um emissor ou um contato de suporte também têm um. Vale a mesma regra que mandou `Cnpj` para `LegalEntities` e `Region` para `Geography`: o VO ganha a área do **conceito**, não da feature que primeiro precisou dele. `Contacts` é também para onde `Telephone` iria, se o modelo normalizado entrar depois.

**Por que a porta fica em `Application/Common/Security` e não na feature:** `ICurrentUser` não é consumida por `Identity` — é consumida por *quem precisar saber quem está agindo*, começando pelo interceptor de auditoria e, em seguida, pelo ledger. É transversal por natureza.

### 2.2 Fluxo
Escrita: `resolver` → `UseCase` (valida cru → VO) → `CommandHandler` (lê-antes-de-escrever por `IRepository` + `Specification`, escreve) → `SaveChanges` → **interceptor carimba auditoria** → Postgres.

Leitura: `resolver` → `UseCase` → `QueryHandler` (`IApplicationDbContext`) → DTO. Estilo A.

---

## 3. Requisitos funcionais

### RF-01 — Criar usuário
`createUser(name, email)` cria um usuário `Active`. O e-mail é normalizado (RN-02) e precisa ser único (RN-01). Não há senha (RN-07).

### RF-02 — Consultar usuário por id
`user(id)` devolve o usuário, ou `null` se não existir. Inativo **é** devolvido — a consulta direta por id não esconde nada; quem esconde é a listagem (RN-03).

### RF-03 — Listar usuários
`users` devolve os usuários **ativos**. Um argumento `includeInactive = false` permite pedir todos.

### RF-04 — Desativar usuário
`deactivateUser(id)` muda o status para `Inactive`. Desativar quem já está inativo é operação sem efeito, não erro.

### RF-05 — Reativar usuário
`activateUser(id)` muda o status para `Active`. Mesma tolerância à repetição.

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
| **RN-10** | **O agregado não sabe que é auditado.** `User` expõe `CreatedAt`/`UpdatedAt`/`CreatedBy`/`UpdatedBy` com `private set` e nunca os atribui. O interceptor escreve via `entry.Property(...).CurrentValue`, que atravessa o setter privado sem o domínio afrouxar encapsulamento. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Auditoria não esquecível):** o carimbo mora num interceptor, não numa chamada em cada handler. Não existe caminho de escrita que "esqueça" de auditar — que é exatamente o modo de falha da abordagem manual.
- **RNF-02 (Tempo injetado):** o interceptor recebe `TimeProvider`, não chama `DateTimeOffset.UtcNow`. Em teste, um `TimeProvider` fake com hora fixa torna a asserção exata; sem isso, verificar "o `UpdatedAt` mudou" viraria `Thread.Sleep` ou tolerância.
- **RNF-03 (Domínio puro):** `IAuditable` é interface marcadora em `Domain/Common`, sem referência a EF — exatamente como `IAggregateRoot`. O mecanismo é 100% Infrastructure.
- **RNF-04 (Porta antes do mecanismo):** nada além do adapter conhece *como* a identidade chega. Quando o JWT entrar, muda um arquivo em `Api/Security`; domínio, use cases e o ledger futuro ficam intactos.
- **RNF-05 (Sem timestamps retroativos):** as entidades existentes (`Holiday`, `FuelStation`, `FuelPrice`) **não** passam a ser auditáveis nesta fatia — seria migration em tabela grande sem demanda. `User` é a primeira e única.

---

## 6. Interface — GraphQL

```graphql
type Query {
  user(id: UUID!): UserResult
  users(includeInactive: Boolean! = false): [UserResult!]!
  me: UserResult
}

type Mutation {
  createUser(input: CreateUserInput!): UserResult!
  deactivateUser(id: UUID!): UserResult!
  activateUser(id: UUID!): UserResult!
}

input CreateUserInput {
  name: String!
  email: String!
}

enum UserStatus { ACTIVE INACTIVE }

type UserResult {
  id: UUID!
  name: String!
  email: String!
  status: UserStatus!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

`CreatedBy`/`UpdatedBy` **não** são expostos no GraphQL. São trilha interna; expor "quem alterou" é decisão de produto que ainda não foi tomada, e é mais fácil adicionar campo depois do que remover.

---

## 7. Segurança — o buraco é consciente e precisa de trava

O `HeaderCurrentUser` lê `X-User-Id` e acredita. Isso é **bypass total de autenticação** — não é uma fraqueza, é a ausência da coisa. O que impede isso de virar incidente é a trava de RN-06:

- O adapter é registrado **apenas** quando `!Environment.IsProduction()`.
- Em Production, nada implementa `ICurrentUser`. Como o `AuditableEntityInterceptor` depende da porta e é um serviço registrado, o `ValidateOnBuild` que o `Program.cs` já liga faz a aplicação **falhar no boot**.
- Para o erro não ser um `InvalidOperationException` críptico de DI, o `Program.cs` lança explicitamente em Production com mensagem dizendo que a autenticação real não foi implementada.

O efeito é que este projeto **não sobe em Production** enquanto não houver autenticação de verdade — o que é a resposta correta, e não um bloqueio a contornar. Enquanto a escolha for "sem auth", o certo é não ter ambiente exposto.

---

## 8. Rastreabilidade (requisito → commit)

*A preencher conforme a implementação avança.*

Ordem de implementação, por dependência:

| # | Commit | Depende de |
|---|---|---|
| 1 | `ICurrentUser` (porta) + `HeaderCurrentUser` (adapter, fora de Production) + trava de boot | — |
| 2 | `IAuditable` + `AuditableEntityInterceptor` + `TimeProvider` no DI | 1 (o interceptor consome a porta) |
| 3 | VO `Email` | — |
| 4 | Agregado `User` + `UserStatus` + `IdentityErrors` | 3 |
| 5 | Mapeamento EF + `EmailConverter` + índice único + migration | 4, 2 |
| 6 | `CreateUser` command/handler/use case + specification | 5 |
| 7 | Queries `user`, `users`, `me` | 5, 1 |
| 8 | `deactivateUser` / `activateUser` | 6 |
| 9 | Wiring DI + GraphQL + funcionais | todos |

## 9. Cobertura de testes planejada

| Nível | Cobre |
|---|---|
| `EmailTests` | normalização (trim, minúsculas); inválidos lançam com o código certo; `TryCreate` nos dois caminhos; igualdade por valor (`A@X.com` == `a@x.com`). |
| `UserTests` | `Create` nasce `Active`; nome em branco lança; desativar/reativar; repetição é no-op; auditoria **não** é escrita pelo agregado. |
| `AuditableEntityInterceptorTests` (Testcontainers) | `Added` carimba os quatro campos; `Modified` mexe só nos `Updated*`; sem usuário corrente grava autoria nula; `TimeProvider` fake torna a asserção exata. |
| `CreateUserCommandHandlerTests` | e-mail duplicado lança `identity.user.email.duplicate` antes de tocar o banco. |
| Persistência (Testcontainers) | o índice único rejeita duplicata **normalizada** (`A@x.com` depois de `a@x.com` → `DbUpdateException`); `EmailConverter` sobrevive ao round-trip. |
| `ListUsersQueryHandlerTests` | inativo fora por padrão, dentro com `includeInactive`. |
| Funcional (GraphQL) | `createUser` → `user` → `deactivateUser` → some de `users`; `me` com header devolve o usuário, sem header devolve `null`; e-mail inválido vira erro com `extensions.code`. |

---

## 10. PR seguinte — autenticação de verdade

- Senha: hash com `PasswordHasher<T>` do ASP.NET Core (Identity) ou BCrypt; nunca artesanal.
- `login(email, password)` devolvendo JWT + refresh token; revogação por `jti`, como no Java (`RefreshToken`, `TokenRevocationService`).
- `JwtCurrentUser` substituindo o `HeaderCurrentUser` — **um arquivo**, se a RNF-04 tiver sido respeitada.
- `[Authorize]` nos resolvers e remoção da trava de boot da RN-06.

## 11. Itens em aberto

- **Contato normalizado** (`UserEmail`/`UserTelephone`/`UserAddress`) — daria consumidor real para a integração ViaCEP, hoje órfã.
- **Tornar as entidades existentes auditáveis** — `FuelPrice` seria o caso com valor (saber quando cada coleta foi ingerida), mas é migration em tabela grande.
- **Expor autoria no GraphQL** — decisão de produto, não técnica.
- **Papéis e autorização** — decidido que entram em fatia própria, depois desta. Hoje não há vocabulário para papel; quando entrar, provavelmente merece ADR (papel no usuário vs. permissão por recurso é escolha com alternativa séria).
