using Financarias.Domain.Identity;

namespace Financarias.Application.UnitTests;

// Hash válido em forma, sem passar pelo Argon2: quem só precisa de um usuário não paga
// ~150 ms por hash. Só o Argon2PasswordHasherTests deriva de verdade.
internal static class TestPasswordHashes
{
    public static PasswordHash Any { get; } = PasswordHash.Create(
        "$argon2id$v=19$m=19456,t=2,p=1$c2FsdGRldGVzdGU$aGFzaGRldGVzdGVxdWVuYW9wcmVjaXNhc2VycmVhbA",
        pepperVersion: 1);
}
