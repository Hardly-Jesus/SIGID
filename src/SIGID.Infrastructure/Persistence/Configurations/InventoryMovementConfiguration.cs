using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", table =>
        {
            table.HasCheckConstraint("CK_InventoryMovements_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_InventoryMovements_StockAfter", "[StockAfter] >= 0");
        });

        builder.HasKey(m => m.Id);

        // Se guarda como texto ("Entry" / "Exit") para que sea legible en la base de datos.
        builder.Property(m => m.MovementType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(m => m.Quantity)
            .IsRequired();

        builder.Property(m => m.Date)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(250);

        builder.Property(m => m.StockAfter)
            .IsRequired();

        // Product 1 ──── N InventoryMovement. Un producto con movimientos no puede borrarse.
        builder.HasOne(m => m.Product)
            .WithMany(p => p.Movements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para los filtros del historial.
        builder.HasIndex(m => new { m.ProductId, m.Date });
        builder.HasIndex(m => m.Date);
    }
}
