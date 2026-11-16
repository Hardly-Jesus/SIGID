using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class SyntheticSalesBatchConfiguration : IEntityTypeConfiguration<SyntheticSalesBatch>
{
    public void Configure(EntityTypeBuilder<SyntheticSalesBatch> builder)
    {
        // Tabla propia: los datos de prueba no se mezclan con las ventas reales.
        builder.ToTable("SyntheticSalesBatches", table =>
        {
            table.HasCheckConstraint("CK_SyntheticSalesBatches_Dates", "[StartDate] <= [EndDate]");
            table.HasCheckConstraint("CK_SyntheticSalesBatches_RandomSeed", "[RandomSeed] >= 0");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CreatedAt).IsRequired();

        builder.Property(b => b.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(b => b.EndDate)
            .IsRequired()
            .HasColumnType("date");

        // Se guarda como texto ("Stable", "Seasonal"...) para que sea legible en la base de datos.
        builder.Property(b => b.Pattern)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.ProductIds)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.ParametersKey)
            .IsRequired()
            .HasMaxLength(600);

        // Un mismo conjunto de parámetros no puede cargarse dos veces.
        builder.HasIndex(b => b.ParametersKey).IsUnique();
    }
}
