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

    public DbSet<Administrator> Administrators => Set<Administrator>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las clases IEntityTypeConfiguration<T> de la carpeta Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SigidDbContext).Assembly);
    }
}
