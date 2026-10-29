using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Date)
            .IsRequired();

        // Identificador del formulario: evita registrar dos veces la misma venta (doble envío).
        builder.Property(s => s.RequestId)
            .IsRequired();

        builder.HasIndex(s => s.RequestId).IsUnique();
        builder.HasIndex(s => s.Date);
    }
}
