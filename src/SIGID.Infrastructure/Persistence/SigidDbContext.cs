using Microsoft.EntityFrameworkCore;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence;

public class SigidDbContext : DbContext
{
    public SigidDbContext(DbContextOptions<SigidDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las clases IEntityTypeConfiguration<T> de la carpeta Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SigidDbContext).Assembly);
    }
}
