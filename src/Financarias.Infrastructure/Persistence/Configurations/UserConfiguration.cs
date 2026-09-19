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
            .HasColumnOrder(1)
            .ValueGeneratedNever();

        builder.Property(u => u.Name)
            .HasColumnOrder(2)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .HasColumnOrder(3)
            .IsRequired()
            .HasMaxLength(254)
            .HasConversion<EmailConverter>();

        builder.Property<string>("EmailHost")
            .HasColumnOrder(4)
            .HasComputedColumnSql("split_part(email, '@', -1)", stored: true);

        builder.ComplexProperty(u => u.PasswordHash, hash =>
        {
            hash.Property(h => h.Value)
                .HasColumnName("password_hash")
                .HasColumnOrder(5)
                .HasMaxLength(256);

            hash.Property(h => h.PepperVersion)
                .HasColumnName("password_pepper_version")
                .HasColumnOrder(6);
        });

        builder.Property(u => u.Status)
            .HasColumnOrder(7)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(u => u.CreatedAt).HasColumnOrder(8);
        builder.Property(u => u.CreatedBy).HasColumnOrder(9);
        builder.Property(u => u.UpdatedAt).HasColumnOrder(10);
        builder.Property(u => u.UpdatedBy).HasColumnOrder(11);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex("EmailHost");
    }
}
