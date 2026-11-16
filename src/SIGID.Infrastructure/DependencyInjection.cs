using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGID.Application.Interfaces.Forecasting;
using SIGID.Application.Interfaces.Persistence;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Security;
using SIGID.Infrastructure.Forecasting;
using SIGID.Infrastructure.Persistence;
using SIGID.Infrastructure.Persistence.Repositories;
using SIGID.Infrastructure.Security;

namespace SIGID.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el DbContext (SQL Server), los repositorios, las transacciones, el hash de contraseñas
    /// y el motor de predicción de demanda.
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
        services.AddScoped<IAdministratorRepository, AdministratorRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();

        // EP-05: datos de prueba y motor de predicción ML.NET/SSA.
        services.AddScoped<ISyntheticSalesRepository, SyntheticSalesRepository>();
        services.AddSingleton<IDemandForecastEngine, SsaDemandForecastEngine>();

        return services;
    }
}
