using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Infrastructure.Persistence;
using SIGID.Infrastructure.Persistence.Repositories;

namespace SIGID.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el DbContext (SQL Server) y los repositorios de la capa de infraestructura.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'DefaultConnection'. Revise appsettings.json o los User Secrets.");

        services.AddDbContext<SigidDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(SigidDbContext).Assembly.FullName)));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();

        return services;
    }
}
