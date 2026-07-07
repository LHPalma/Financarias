using Financarias.Domain.Addresses;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Financarias.Infrastructure.Persistence.Converters;

public sealed class CepConverter() : ValueConverter<Cep, string>(
    cep => cep.Value,
    value => Cep.Create(value));
