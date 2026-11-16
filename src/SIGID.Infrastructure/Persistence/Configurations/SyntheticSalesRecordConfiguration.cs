using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class SyntheticSalesRecordConfiguration : IEntityTypeConfiguration<SyntheticSalesRecord>
{
    public void Configure(EntityTypeBuilder<SyntheticSalesRecord> builder)
    {
        builder.ToTable("SyntheticSalesRecords", table =>
        {
            table.HasCheckConstraint("CK_SyntheticSalesRecords_Quantity", "[Quantity] > 0");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Date)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(r => r.Quantity).IsRequired();

        // Al eliminar un lote se eliminan sus registros (solo datos de prueba).
        builder.HasOne(r => r.Batch)
            .WithMany(b => b.Records)
            .HasForeignKey(r => r.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Los registros deben corresponder a productos existentes.
        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un registro por producto y día dentro de cada lote.
        builder.HasIndex(r => new { r.BatchId, r.ProductId, r.Date }).IsUnique();

        // Índice para las consultas por producto y período.
        builder.HasIndex(r => new { r.ProductId, r.Date });
    }
}
