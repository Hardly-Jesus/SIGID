using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class AdministratorConfiguration : IEntityTypeConfiguration<Administrator>
{
    public void Configure(EntityTypeBuilder<Administrator> builder)
    {
        builder.ToTable("Administrators");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(150);

        // Solo se guarda el hash generado por PasswordHasher, nunca la contraseña.
        builder.Property(a => a.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(a => a.UserName).IsUnique();
        builder.HasIndex(a => a.Email).IsUnique();
    }
}
