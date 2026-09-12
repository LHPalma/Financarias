using Financarias.Domain.Identity;
using Financarias.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254)
            .HasConversion<EmailConverter>();

        builder.ComplexProperty(u => u.PasswordHash, hash =>
        {
            hash.Property(h => h.Value)
                .HasColumnName("password_hash")
                .HasMaxLength(256);

            hash.Property(h => h.PepperVersion)
                .HasColumnName("password_pepper_version");
        });

        builder.Property(u => u.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property<string>("EmailHost")
            .HasComputedColumnSql("split_part(email, '@', -1)", stored: true);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex("EmailHost");
    }
}
