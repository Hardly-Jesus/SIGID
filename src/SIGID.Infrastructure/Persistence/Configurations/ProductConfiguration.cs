using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            // Restricciones de stock
            table.HasCheckConstraint("CK_Products_MinimumStock", "[MinimumStock] >= 0");
            table.HasCheckConstraint("CK_Products_MaximumStock", "[MaximumStock] >= [MinimumStock]");

            // Última protección: las existencias nunca pueden quedar negativas.
            table.HasCheckConstraint("CK_Products_CurrentStock", "[CurrentStock] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.MinimumStock)
            .IsRequired();

        builder.Property(p => p.MaximumStock)
            .IsRequired();

        // Existencias actuales: los productos nuevos empiezan en 0.
        builder.Property(p => p.CurrentStock)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Category 1 ──── N Product
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supplier 1 ──── N Product
        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
