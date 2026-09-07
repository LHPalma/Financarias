using Financarias.Domain.Contacts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Financarias.Infrastructure.Persistence.Converters;

public sealed class EmailConverter() : ValueConverter<Email, string>(
    email => email.Value,
    value => Email.Create(value));
