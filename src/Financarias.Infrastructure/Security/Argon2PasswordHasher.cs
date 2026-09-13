using Financarias.Application.Common.Security;
using Financarias.Domain.Identity;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;

namespace Financarias.Infrastructure.Security;

public class Argon2PasswordHasher(
    IOptions<PasswordHashingOptions> options
) : IPasswordHasher
{
    private const int MemoryCostKib = 19456;
    private const int TimeCost = 2;
    private const int Parallelism = 1;
    private const int HashLengthBytes = 32;

    private PasswordHash? _decoy;

    public PasswordHash Hash(Password password)
    {
        var version = options.Value.CurrentPepperVersion;

        return PasswordHash.Create(
            Argon2.Hash(
                password: password.Value,
                secret: options.Value.Peppers[version],
                timeCost: TimeCost,
                memoryCost: MemoryCostKib,
                parallelism: Parallelism,
                type: Argon2Type.HybridAddressing, // HybridAddressing é o argon2id
                hashLength: HashLengthBytes
            ),
            version);
    }

    public bool Verify(PasswordHash hash, string password)
    {
        if (hash is null)
        {
            _decoy ??= Hash(Password.Create("Descartavel-games-e-jogos-7!"));
            _ = Verify(_decoy, password);
            return false;
        }

        if (!options.Value.Peppers.TryGetValue(hash.PepperVersion, out var pepper))
        {
            throw new InvalidOperationException(
                $"Pepper version {hash.PepperVersion} is not configured; existing hashes cannot be verified.");
        }

        return Argon2.Verify(hash.Value, password, pepper);
    }
}