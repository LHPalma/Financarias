# SRS — Autenticação: senha, login e JWT de acesso

- **Data:** 2026-09-07
- **Feature:** `Identity/Users` (credencial e login) + autenticação transversal (`Api/Security`, `Infrastructure/Security`)
- **Status:** especificado — implementação não iniciada
- **Depende de:** `docs/srs/0004-identity-users.md` (entregue, PR #28)
- **Fonte externa:** nenhuma

---

## 1. Introdução

### 1.1 Propósito
Trocar identidade *declarada* por identidade *provada*. A fatia anterior entregou o vocabulário — `User`, `ICurrentUser`, auditoria — com um adapter que lê `X-User-Id` e acredita. Esta entrega a credencial, o fluxo de login e o token assinado que substitui esse adapter, e com isso remove a trava que impede a aplicação de subir em Production.

### 1.2 Escopo desta entrega
- Credencial: hash Argon2id na criação do usuário, verificação no login.
- `login(email, password)` devolvendo um **JWT de acesso**.
- `JwtCurrentUser` substituindo o `HeaderCurrentUser`.
- `[Authorize]` nos resolvers, em modo binário (autenticado ou não).
- Remoção da trava de boot da RN-06 da fatia anterior.
- `Options` de JWT validadas no boot.

**Fora de escopo, planejado para a fatia seguinte (§10):** refresh token, rotação, revogação por `jti`, logout.

**Fora de escopo indefinidamente nesta rodada:** troca de senha, recuperação por e-mail, verificação de e-mail, bloqueio por tentativas, 2FA, login externo.

### 1.3 Definições
| Termo | Significado |
|---|---|
| **Argon2id** | Função de derivação de chave vencedora da Password Hashing Competition. Híbrida: resistente a GPU (memória) e a ataque de canal lateral. |
| **String PHC** | Formato padrão que carrega algoritmo, versão, parâmetros e salt no próprio valor. Dispensa coluna separada de salt. |
| **JWT de acesso** | Token assinado, curto, que o cliente manda em `Authorization: Bearer`. Carrega o id do usuário na claim `sub`. |
| **HS256** | Assinatura simétrica: a mesma chave assina e verifica. |
| **`jti`** | Identificador único do token. Só importa quando houver revogação (§10). |

---

## 2. Descrição geral

### 2.1 Posição na arquitetura
```
Domain/Identity
├── Password                    (VO — senha crua que já passou pela política de composição)
├── PasswordHash                (VO — envolve a string PHC; o domínio nunca vê texto puro)
└── User                        (+ PasswordHash)

Application/Common/Security
├── ICurrentUser                (já existe — inalterada)
├── IPasswordHasher             (porta: Hash(texto) / Verify(hash, texto))
├── IAccessTokenIssuer          (porta: Issue(Guid userId) → IssuedAccessToken)
└── IssuedAccessToken           (record: Token + ExpiresAt)

Application/Identity/Users
├── DTOs/Requests/LoginRequest
├── DTOs/Results/AccessTokenResult
├── Commands/{Login}{Command, CommandHandler}
└── UseCases/{ILoginUseCase, LoginUseCase}

Infrastructure/Security
├── PasswordHashingOptions      (pepper — validadas no boot)
└── Argon2PasswordHasher        (Isopoh, com o pepper no parâmetro secret)

Api/Security
├── JwtCurrentUser              (substitui HeaderCurrentUser)
├── JwtOptions                  (validadas no boot)
├── JwtAccessTokenIssuer        (JsonWebTokenHandler, HS256)
└── SecurityDependencyInjection (AddAccessTokens)
```

**Por que `IPasswordHasher` e `IAccessTokenIssuer` são portas da Application:** a mesma regra que já pôs `ICurrentUser` lá. O caso de uso de login precisa saber *que* existe verificação de senha e emissão de token; não precisa saber que é Argon2id nem que é JWT.

**Por que `Argon2PasswordHasher` vai em `Infrastructure` e não em `Integrations`:** `Integrations` é para adaptador de API externa. Hash é capacidade técnica local, sem rede. Estica levemente a descrição de `Infrastructure` como "persistência", e é o encaixe menos ruim — criar um projeto só para isso seria cerimônia.

### 2.2 Fluxo
Login: `resolver` → `LoginUseCase` (valida cru → VO) → `LoginCommandHandler` (busca por e-mail via `Specification`, verifica hash por `IPasswordHasher`, emite por `IAccessTokenIssuer`) → `AccessTokenResult`.

Requisição autenticada: `Authorization: Bearer` → middleware do ASP.NET valida assinatura e expiração → `ClaimsPrincipal` → `JwtCurrentUser` lê `sub` → o resto do sistema (auditoria, `me`, ledger futuro) continua vendo apenas `Guid?`.

---

## 3. Requisitos funcionais

### RF-01 — Criar usuário com senha
`createUser(input: { name, email, password })` passa a exigir senha, que precisa cumprir a política da RN-11. O hash é calculado no caso de uso; o domínio recebe apenas o `PasswordHash`.

### RF-02 — Login
`login(input: { email, password })` devolve um JWT de acesso e o instante de expiração. Credencial inválida lança `identity.credentials.invalid`.

### RF-03 — Identidade provada
`ICurrentUser` passa a resolver o id a partir da claim `sub` do token validado. Nenhum consumidor muda.

### RF-04 — Resolvers protegidos
Toda query e mutation de identidade exige autenticação, **exceto** `login` e `createUser`.

### RF-05 — A aplicação sobe em Production
Removida a trava da RN-06 anterior. Em Production o `JwtCurrentUser` é registrado como em qualquer outro ambiente.

---

## 4. Regras de negócio

| ID | Regra |
|---|---|
| **RN-01** | **O domínio nunca vê senha em texto puro.** `User.Create` recebe `PasswordHash`, não `string`. Quem transforma texto em hash é o caso de uso, pela porta. O VO existe justamente para tornar impossível passar um texto puro onde se espera um hash — erro que um `string` deixaria compilar. |
| **RN-02** | **`password_hash` é `NOT NULL`.** Não há usuário sem credencial. A migration **apaga as linhas existentes de `users`** antes de criar a coluna: são exclusivamente lixo de teste, e a trava de boot da fatia anterior garante que nunca existiu banco de produção. É a última janela em que isso é seguro — depois de existir usuário real, a coluna teria de nascer anulável com backfill. A coluna é dimensionada com folga (256), não colada no tamanho atual (97 com os parâmetros da RN-10): subir o custo muda o comprimento da string PHC. |
| **RN-03** | **Login não revela se o e-mail existe.** E-mail inexistente e senha errada devolvem o **mesmo** código e a mesma mensagem, e percorrem o mesmo caminho. Distinguir os dois transforma o login em oráculo de cadastro. |
| **RN-04** | **Usuário inativo não entra.** `Status == Inactive` recebe o mesmo erro genérico da RN-03. Desativar precisa efetivamente barrar o acesso, senão vira só um rótulo. |
| **RN-05** | **A chave de assinatura vem de configuração e é validada no boot.** `Options` com `ValidateOnStart`: chave ausente, vazia ou curta demais derruba a aplicação com mensagem explícita. Sem isso, uma chave vazia assina tokens que qualquer um forja — falha silenciosa e catastrófica. Padrão novo no repo; a partir daqui vale para as demais `Options`. **De onde ela vem:** em produção, variável de ambiente `Jwt__SigningKey`, que o `CreateBuilder` lê depois do `appsettings` e portanto vence. Nunca no `appsettings.json`, que é versionado. Em desenvolvimento, a chave está exposta de propósito no `appsettings.Development.json` (RN-16), e não em user-secrets como esta regra dizia antes. A chave é base64 de ao menos 32 bytes aleatórios, e a validação mede os bytes decodificados: uma frase de 32 caracteres passaria pela contagem de caracteres com entropia de frase. |
| **RN-06** | **HS256, não RSA.** A mesma aplicação assina e valida; não há terceiro verificando. Assinatura assimétrica só se paga quando quem valida não pode ter a chave de assinar, o que não é o caso. Trocar depois é mudança de uma porta. |
| **RN-07** | **Token de 1 hora.** Sem refresh nesta fatia, o cliente refaz login ao expirar. Um token curto demais seria hostil sem renovação; um longo demais amplia a janela de um token vazado. Uma hora é o meio-termo enquanto não há revogação. |
| **RN-08** | **`createUser` e `login` continuam públicos.** Um é cadastro, o outro é a porta. Todo o resto exige autenticação (RF-04). |
| **RN-09** | **Autorização continua binária.** `[Authorize]` sem papel: qualquer usuário autenticado enxerga `users` inteiro. Limitação consciente desta fatia — papéis são fatia própria, com ADR, porque "papel no usuário" versus "permissão por recurso" é escolha com alternativa séria. |
| **RN-10** | **Parâmetros do Argon2id: 19 MiB, 2 iterações, paralelismo 1 — medidos, não copiados.** A string PHC carrega memória, iterações e paralelismo, então subir o custo depois não invalida hashes antigos: verifica-se com os parâmetros gravados. A primeira versão desta regra dizia 64 MiB/t=3, número comum em recomendações que pressupõem implementação **nativa**. Medido com a Isopoh, que é 100% gerenciada, numa máquina de 8 núcleos: 64 MiB/t=3/p=1 custa **~900 ms para gerar e ~1200 ms para verificar** — inaceitável num login, e vetor de indisponibilidade (dez tentativas simultâneas queimam 10 s de CPU e 640 MiB). Com 19 MiB/t=2/p=1 — que é a configuração **mínima recomendada pela OWASP** para Argon2id, não um afrouxamento de conveniência — o custo cai para **~150 ms**, dentro da faixa usual de alvo. Paralelismo fica em 1: p=4 desce para ~100 ms, mas consome quatro threads por login e piora a vazão sob concorrência, que é o oposto do que se quer num servidor. |
| **RN-02b** | **Migration que adiciona coluna `NOT NULL` precisa ser revisada à mão.** Ao gerar `AddPasswordToUsers`, o EF colocou `defaultValue: ""` no hash e `defaultValue: 0` na versão. Com isso a migration **passaria** sobre uma tabela com linhas e deixaria usuários com hash vazio e pepper versão 0 — e o erro só apareceria no primeiro login, porque o EF materializa `PasswordHash` pelo construtor, sem passar pela validação do `Create`. Corrigido com `DELETE FROM users` antes e sem os defaults; verificado sobre uma tabela com dados que as linhas somem e que `INSERT` sem hash é rejeitado pelo banco. Os testes de integração não pegam isso: todos partem de banco vazio. |
| **RN-11** | **Política de composição: 8 a 128 caracteres, com maiúscula, minúscula, dígito e caractere especial.** Especial é qualquer símbolo ASCII imprimível — os 32 que não são letra nem dígito. Espaço é **aceito** na senha (frase de senha é caso legítimo) mas **não satisfaz** o requisito de especial, senão um espaço acidental no fim transformaria senha fraca em "forte". Senha nunca é aparada: `Trim()` mudaria a credencial de quem usa espaço na ponta — o oposto do `Email`, onde normalizar é obrigatório. A ordem das checagens define qual erro aparece quando mais de uma regra falha: comprimento, depois maiúscula, minúscula, dígito e especial. Cada regra tem código próprio, para o cliente poder dizer *o que* corrigir. |
| **RN-12** | **A divergência do NIST é consciente e documentada.** O NIST SP 800-63B diz que verificadores **não devem** impor regras de composição, e a OWASP acompanha. O motivo é empírico: as regras empurram para padrões previsíveis — `Password1!` satisfaz as quatro e está em qualquer dicionário de ataque —, rejeitam frases longas e fortes que não têm símbolo, e estimulam reuso da mesma senha "que atende às regras". O controle que as substituiria com ganho real é conferir contra lista de senhas vazadas (§11). A RN-11 foi mantida assim por **decisão explícita do autor, com objetivo didático**: implementar e entender a política faz parte do escopo de aprendizado do trabalho. Fica registrado para que a escolha se leia como escolha, não como desconhecimento. |
| **RN-13** | **Pepper pelo parâmetro `secret` do Argon2, não por concatenação.** O Argon2 tem um segredo opcional na própria especificação (o `K` da RFC 9106), e a Isopoh o expõe — então o pepper entra na derivação como o algoritmo prevê, em vez de ser colado na senha antes de hashear. Verificado: o pepper **não aparece** na string PHC, o formato fica **indistinguível** do de um sistema sem pepper, e `Verify` só devolve verdadeiro com o pepper certo — sem ele, ou com outro, falha. O efeito é que um dump do banco, sozinho, é inatacável offline. A chave segue a mesma regra da RN-05 — variável de ambiente em produção, e ausência derruba o boot —, com a exceção declarada de desenvolvimento da RN-16. O pepper é versionado (RN-15). **A porta `IPasswordHasher` não muda**: `Hash(Password)` e `Verify(PasswordHash, string)` continuam idênticos, e o pepper vive inteiramente dentro do adapter — nenhum caso de uso, teste de aplicação ou resolver fica sabendo que ele existe. |
| **RN-14** | **Pepper e salt não se substituem.** O salt é por senha, público, guardado junto do hash, e serve para derrotar rainbow table e para impedir que dois usuários com a mesma senha tenham o mesmo hash. O pepper é global, secreto, guardado fora do banco, e serve para tornar o ataque offline inviável quando **só** o banco vaza. Remover um por achar que o outro cobre é erro de categoria. |
| **RN-15** | **O pepper é versionado, e a versão pertence ao hash.** Para verificar uma senha são necessárias duas coisas — a string PHC **e** a versão do pepper que a derivou —, então elas formam um valor só: `PasswordHash` carrega `Value` e `PepperVersion`, e versão menor que 1 é inválida (versão zero é exatamente o que uma propriedade esquecida sem atribuição teria). A configuração deixa de ser um pepper e vira um mapa `versão → pepper` com uma `CurrentPepperVersion`: `Hash` usa sempre a corrente e grava a versão; `Verify` busca o pepper pela versão gravada. Validado no boot: mapa não vazio, versão corrente presente no mapa, todo pepper com ao menos 32 caracteres — inclusive versões antigas. **Versão gravada que não está no mapa lança**, não devolve falso: é erro de configuração (um pepper aposentado cedo demais), e devolver falso faria todo usuário afetado ver "senha incorreta" em silêncio. As duas colunas persistem juntas; o mapeamento (`OwnsOne` versus coluna separada) é decidido no passo 2. |
| **RN-16** | **O pepper e a chave de assinatura de desenvolvimento são expostos de propósito, para aprendizado.** Ambos estão versionados em `appsettings.Development.json`, cada um com comentário explícito. Um segredo só protege enquanto é segredo, então nenhum dos dois protege nada — existem para a aplicação subir em Development sem configuração extra e para estudar as técnicas. É exceção consciente à RN-05 e à regra de nunca versionar segredo, decidida pelo autor. **Os dois não pesam igual:** o pepper exposto, sozinho, não dá acesso a nada; a chave de assinatura exposta permite **forjar token válido como qualquer usuário**, então vale contra qualquer instância rodando em Development acessível pela rede. O que contém os dois: o arquivo **só é carregado em Development**, então em Production o `ValidateOnStart` continua derrubando o boot até existirem valores configurados fora do repositório. **Em produção, ambos devem ser tratados como comprometidos**: não reutilizar, gerar valores novos por variável de ambiente (`PasswordHashing__Peppers__N`, `Jwt__SigningKey`) e rotacionar. Os funcionais sobem em `Testing`, que não lê esse arquivo, e recebem valores de teste por variável de ambiente no `TestBootstrap`. |

---

## 5. Requisitos não-funcionais

- **RNF-01 (Uma linha de troca):** a promessa da fatia anterior é cobrada aqui. Substituir `HeaderCurrentUser` por `JwtCurrentUser` deve tocar **apenas** `Api/Security` e o registro no DI. Se algum caso de uso, handler ou o interceptor de auditoria precisar mudar, a fatia anterior falhou — e isso deve ser registrado, não contornado.
- **RNF-02 (Primitivas de biblioteca):** o modelo é nosso; as primitivas não. Argon2id vem de `Isopoh.Cryptography.Argon2`; assinatura e validação de JWT, de `Microsoft.AspNetCore.Authentication.JwtBearer`; aleatoriedade, de `RandomNumberGenerator`. Nenhuma comparação, derivação ou geração escrita à mão.
- **RNF-03 (Verificação em tempo constante):** comparação de hash não pode vazar tempo. É responsabilidade da biblioteca; o adapter não implementa comparação própria.
- **RNF-04 (Senha nunca em log nem em erro):** texto puro não aparece em mensagem de exceção, log ou resposta. O erro de credencial inválida não ecoa a entrada — diferente de `ContactsErrors.Email`, que ecoa por ser dado inócuo.
- **RNF-05 (Custo de hash é sentido no teste):** Argon2id é lento de propósito — ~150 ms por operação com os parâmetros da RN-10. Testes que criam muitos usuários usam um `IPasswordHasher` substituto; só os testes do próprio adapter exercitam o algoritmo real.

---

## 6. Interface — GraphQL

```graphql
type Mutation {
  createUser(input: CreateUserRequestInput!): UserResult!
  login(input: LoginRequestInput!): AccessTokenResult!
}

input CreateUserRequestInput {
  name: String!
  email: String!
  password: String!
}

input LoginRequestInput {
  email: String!
  password: String!
}

type AccessTokenResult {
  accessToken: String!
  expiresAt: DateTime!
}
```

`AccessTokenResult` **não** devolve o usuário junto. Quem quiser os dados chama `me` com o token — é uma ida a mais e mantém o login com uma responsabilidade só.

Nenhum campo de `User` ou `UserResult` expõe o hash. O `UserType` usa `BindFieldsExplicitly()`, então uma propriedade nova no agregado não vaza por descuido; o `UserResult`, montado à mão, também não a inclui.

---

## 7. Segurança — o que esta fatia fecha e o que continua aberto

**Fecha:** identidade deixa de ser declarada. Não é mais possível agir como outro usuário mandando um header.

**Continua aberto, e é consciente:**
- **Sem revogação.** Um token vazado vale até expirar; não há logout de servidor. É o que a fatia seguinte resolve, e é o principal motivo de o token ser de uma hora e não de um dia.
- **Autorização binária.** Qualquer autenticado lista todos os usuários (RN-09).
- **Sem bloqueio por tentativas.** Nada impede força bruta no `login` além do custo do Argon2id, que é real mas não é limite de taxa.
- **Sem verificação de e-mail.** Alguém pode se cadastrar com e-mail de terceiro.
- **O pepper protege contra um tipo de brecha, não contra todas.** Ele perde o valor se o atacante levar **também** o servidor de aplicação, porque é lá que a chave vive. Defende exfiltração do banco isolada — que é a forma mais comum, mas não a única.
- **Perder o pepper torna toda senha inverificável, sem recuperação.** Nem reset por e-mail resolve o passado: os hashes antigos viram lixo permanente. O backup dessa chave passa a importar tanto quanto o backup do banco, e isso é risco operacional que o sistema não tinha antes.

Nenhum desses impede a aplicação de subir, e por isso a trava sai. Mas os quatro devem estar escritos aqui antes de existir usuário real.

---

## 8. Rastreabilidade (requisito → commit)

*A preencher conforme a implementação avança.*

| # | Commit | Depende de |
|---|---|---|
| 1 | VO `PasswordHash` + `IPasswordHasher` + `Argon2PasswordHasher` + pacote | — |
| 1b | VO `Password` + política de composição + `IPasswordHasher.Hash(Password)` | 1 |
| 1c | pepper versionado: `PasswordHash` com `PepperVersion`, `PasswordHashingOptions` com mapa de versões validado no boot (`ValidateOnStart` estreia no repo), adapter usando o `secret`, pepper de aprendizado em `appsettings.Development.json` | 1 |
| 2+3 | `User` exige `PasswordHash`, mapeado por `ComplexProperty` em `password_hash` + `password_pepper_version`; migration com `DELETE` e sem defaults; `createUser` recebe a senha, o caso de uso valida (`Password.Create`) e hasheia pela porta. **Os passos 2 e 3 viraram um só**: o caso de uso monta o comando, e sem a senha não há de onde tirar o hash — não existe estado intermediário que compile | 1c |
| 4 | `JwtOptions` validadas no boot + `IAccessTokenIssuer` + `JwtAccessTokenIssuer` + `AddAccessTokens`. **Desvio do especificado:** a porta recebe `Guid userId`, não `User` — o token só carrega o id, e a porta espelha o `ICurrentUser`, que devolve `Guid?`; receber o agregado inteiro daria à porta acesso a e-mail, nome e hash sem que nada precise disso | — |
| 5 | `login` command/handler/use case + `AccessTokenResult` | 2+3, 4 |
| 6 | `AddAuthentication`/`UseAuthentication` + `JwtCurrentUser` + remoção da trava | 4 |
| 7 | `[Authorize]` nos resolvers + funcionais ponta a ponta | 5, 6 |

---

## 9. Cobertura de testes planejada

| Nível | Cobre |
|---|---|
| `PasswordHashTests` | rejeita vazio e formato que não é PHC; igualdade por valor |
| `PasswordHashingOptionsTests` | usa o `AddInfrastructure` real e o mesmo `IStartupValidator` que o host executa: configuração válida passa; sem pepper, versão corrente fora do mapa e pepper curto (inclusive em versão não corrente) derrubam o boot, cada um com sua mensagem |
| `Argon2PasswordHasherTests` | algoritmo e custo travados no teste; o hash grava a versão corrente; o pepper não aparece na string PHC; salt aleatório; senha errada não verifica; **outro pepper na mesma versão não verifica, nem hash derivado sem pepper**; **rotação** — hash da v1 continua verificando com a corrente em v2, e os novos saem em v2; versão não configurada **lança**; e `Verify` aceita senha que não passaria na política de hoje |
| `PasswordHashTests` | preserva string e versão; entrada que não é PHC e versão não positiva lançam `identity.passwordhash.invalid`; igualdade considera a versão; `ToString` não vaza |
| `PasswordTests` | as seis regras da RN-11, cada uma com seu código; o conjunto de especiais conferido contra a regra que ele codifica (ASCII imprimível não alfanumérico); espaço aceito mas não contando como especial; `ToString` não vaza |
| `UserTests` | `Create` exige `PasswordHash`; o agregado não expõe o hash |
| `LoginCommandHandlerTests` | credencial certa emite token; e-mail inexistente, senha errada e usuário inativo devolvem **o mesmo** código (RN-03/RN-04) |
| `JwtAccessTokenIssuerTests` | `sub` com o id, `iss`, `aud` e `jti`; cabeçalho `alg: HS256` — `SecurityAlgorithms.HmacSha256Signature` gravaria a URI do XML-DSig no `alg` com o `JsonWebTokenHandler`, verificado; `iat`, `nbf` e `exp` exatos com relógio falso; `jti` diferente a cada emissão; assinatura confere com a chave e não confere com outra |
| `JwtOptionsTests` | pelo `AddAccessTokens` real e o `IStartupValidator`: configuração válida passa; issuer ou audience ausente, chave vazia, não base64 ou com 32 caracteres base64 que decodificam para só 24 bytes, e validade zero ou negativa derrubam o boot; sem validade configurada, o padrão é uma hora |
| `JwtCurrentUserTests` | lê `sub` do `ClaimsPrincipal`; sem principal devolve nulo |
| Funcional (GraphQL) | `createUser` → `login` → `me` com Bearer devolve o usuário; sem token, `me` e `users` devolvem erro de autenticação; `login` errado devolve `identity.credentials.invalid` |
| Auditoria (Testcontainers) | escrita feita com token carimba `CreatedBy` com o id do token — a prova de que a RNF-01 valeu |

---

## 10. Fatia seguinte — refresh token e revogação

- Agregado `RefreshToken` persistido, com rotação a cada uso.
- Detecção de reuso: um refresh já rotacionado apresentado de novo indica roubo — política provável é revogar a família inteira, e isso merece discussão própria.
- Revogação por `jti`, e logout de servidor.
- Encurtar o access token para 15 minutos, que só faz sentido quando há renovação.

---

## 11. Itens em aberto

- **ADR "identidade própria em vez de ASP.NET Core Identity"** — pendente desde a fatia anterior, e esta é onde a alternativa rejeitada fica mais visível: `PasswordHasher<T>`, `SignInManager` e `UserManager` são exatamente o que estamos reconstruindo.
- **Papéis e autorização** — fatia própria, provavelmente com ADR.
- **Conferir senha contra lista de vazadas** — o controle que o NIST recomenda no lugar das regras de composição (RN-12). A API do Have I Been Pwned faz isso por k-anonimato: manda-se os 5 primeiros caracteres do SHA-1 e recebem-se os sufixos que batem, sem o serviço nunca ver a senha.
- **Re-hash no login após rotação** — com o pepper versionado (RN-15), rotacionar já é **possível**: adiciona-se a nova versão ao mapa, aponta-se a corrente para ela, e os hashes antigos continuam verificando. O que falta é migrar os antigos: no login bem-sucedido, quando `hash.PepperVersion` difere da corrente, derivar de novo e regravar, já que é o único momento em que a senha em texto puro está disponível. Só depois de ninguém mais usar uma versão ela pode sair do mapa — antes disso, a RN-15 faz o `Verify` lançar. Pertence ao passo 5 (`login`) ou a uma fatia própria.
- **Bloqueio por tentativas** no login.
- **Verificação de e-mail** no cadastro.
- **Troca e recuperação de senha.**
- **`ValidateOnStart` nas demais `Options`** — o padrão nasce aqui; o simulador de financiamento já o queria (`docs/srs/0003-financing-price.md` §10).
