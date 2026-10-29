using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
{
    public void Configure(EntityTypeBuilder<SaleDetail> builder)
    {
        builder.ToTable("SaleDetails", table =>
        {
            table.HasCheckConstraint("CK_SaleDetails_Quantity", "[Quantity] > 0");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Quantity)
            .IsRequired();

        // Sale 1 ──── N SaleDetail. Las ventas son historial: no se borran en cascada.
        builder.HasOne(d => d.Sale)
            .WithMany(s => s.Details)
            .HasForeignKey(d => d.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un producto aparece una sola vez en cada venta.
        builder.HasIndex(d => new { d.SaleId, d.ProductId }).IsUnique();
        builder.HasIndex(d => d.ProductId);
    }
}
