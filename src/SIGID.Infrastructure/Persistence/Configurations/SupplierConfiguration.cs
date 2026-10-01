using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Identification)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.Phone)
            .HasMaxLength(30);

        builder.Property(s => s.Email)
            .HasMaxLength(150);

        builder.Property(s => s.Address)
            .HasMaxLength(250);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(s => s.Identification)
            .IsUnique();
    }
}
