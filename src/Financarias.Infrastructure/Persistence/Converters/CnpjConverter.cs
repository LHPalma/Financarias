using Financarias.Domain.LegalEntities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Financarias.Infrastructure.Persistence.Converters;

public sealed class CnpjConverter() : ValueConverter<Cnpj, string>(
    cnpj => cnpj.Value,
    value => Cnpj.Create(value));
