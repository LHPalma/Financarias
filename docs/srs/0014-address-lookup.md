# SRS — Consulta de endereço por CEP

- **Data:** 2026-10-11 (escrito depois da entrega, que é de 24 a 25/06/2026; o código é a fonte da verdade e este documento registra o que foi construído)
- **Feature:** `Addresses` (`Domain/Addresses`, `Application/Addresses`, `Integrations/Addresses/ViaCep`)
- **Status:** **entregue.** É o primeiro slice do projeto e o que fixou o padrão de integração por Refit.
- **Fonte externa:** [ViaCEP](https://viacep.com.br) (`/ws/{cep}/json`), sem chave.

---

## 1. Introdução

### 1.1 Propósito
Devolver logradouro, bairro, município, UF e complemento de um CEP. Foi escolhido como primeiro slice por ser pequeno e completo: tem um VO, um caso de uso, uma porta, um adaptador HTTP e um erro de domínio com código.

### 1.2 Escopo
Dentro: a query `addressLookup(cep)`. Fora: busca reversa (endereço para CEP), cache, outras fontes (Correios, BrasilAPI), e persistência. O endereço **não é guardado**: cada consulta vai à fonte.

## 2. Requisitos

### RF-01 — Consultar o endereço de um CEP
`addressLookup(cep: String!): AddressLookupResult`. O CEP pode vir com ou sem máscara (`01001-000`, `01001000`).

### RN-01 — CEP é um VO que descarta tudo que não é dígito
`Cep` (`Domain/Addresses`) é um `record` com construtor privado e as fábricas `Create` (lança) e `TryCreate`. Tira os caracteres que não são dígito e exige **exatamente 8**. `Formatted` devolve `01001-000`. Entrada inválida lança `address.cep.invalid`, que o `DomainErrorFilter` entrega como `extensions.code`.

### RN-02 — CEP inexistente é nulo, não erro
Se o ViaCEP responde `{"erro": true}` (o campo chega como booleano ou como texto, e um conversor de JSON tolerante trata os dois), a consulta devolve `null`. Um CEP bem formado que não existe é um resultado válido da consulta.

### RN-03 — Falha de transporte também vira nulo
O provedor captura qualquer exceção, registra em log e devolve `null`. **Isso confunde "CEP não existe" com "ViaCEP fora do ar"**: o cliente não distingue. Foi aceito para o primeiro slice, em que o endereço é uma conveniência, e continua assim.

### RN-04 — Campos ausentes são nulos
`AddressLookupResult(Street, Neighborhood, City, State, Complement)`: todos anuláveis. A tradução dos nomes do ViaCEP (`logradouro`, `bairro`, `localidade`, `uf`, `complemento`) acontece no provedor, que é a camada anticorrupção. O DTO de resposta (`ViaCepResponse`) mantém os nomes em português da fonte.

## 3. Camadas
```
addressLookup (resolver) → IFindAddressByCepUseCase  : valida com Cep.Create
                         → IQueryHandler<FindAddressByCepQuery, AddressLookupResult?>
                         → IAddressLookupProvider (porta)  ← ViaCepProvider ← IViaCepClient (Refit)
```
`Integrations:ViaCep:BaseUrl` vem da configuração; a ausência derruba o boot.

## 4. Rastreabilidade
| Commit | Entrega |
|---|---|
| `5bcf660` | Cliente Refit e provedor do ViaCEP |
| `a5f3861` | `addressLookup` no GraphQL |
| `91a602d`, `56586c8` | Testes do cliente e do provedor, da desserialização e do conversor booleano |
| `4af648a` | VO `Cep` |
| `6b7c57a`, `9f42ea5` | Caso de uso e seus testes |
| `d91b19f` | `DomainErrorFilter`: erro de domínio vira erro GraphQL com código |

## 5. Testes
Domínio: `Cep` (máscara, tamanho, vazio). Aplicação: o caso de uso valida e delega. Integrações: provedor com cliente simulado (encontrado, `erro`, exceção), desserialização incluindo o booleano em texto, e um teste de contrato contra o ViaCEP real, fora do CI principal.

## 6. Limites conhecidos
- RN-03: indisponibilidade da fonte é indistinguível de CEP inexistente.
- Sem cache: cada chamada é uma ida ao ViaCEP.
- É uma integração **sem consumidor interno**: nenhum agregado usa endereço ainda. O "contato normalizado do usuário" (`UserAddress`) seria o primeiro.
