using Financarias.Application.Common.Security;
using Financarias.Domain.Identity;
using Isopoh.Cryptography.Argon2;

namespace Financarias.Infrastructure.Security;

public class Argon2PasswordHasher
    : IPasswordHasher
{
    private const int MemoryCostKib = 19456;
    private const int TimeCost = 2;
    private const int Parallelism = 1;
    private const int HashLengthBytes = 32;

    public PasswordHash Hash(string password)
    {
        return PasswordHash.Create(Argon2.Hash(
            password,
            timeCost: TimeCost,
            memoryCost: MemoryCostKib,
            parallelism: Parallelism,
            // HybridAddressing é o argon2id; a Isopoh nomeia os tipos pelo modo de acesso à memória.
            type: Argon2Type.HybridAddressing,
            hashLength: HashLengthBytes
        ));
    }

    public bool Verify(PasswordHash hash, string password) => Argon2.Verify(hash.Value, password);
}