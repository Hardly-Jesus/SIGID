using SIGID.Application.DTOs.Authentication;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Services;

namespace SIGID.Web.Security;

/// <summary>
/// Crea la cuenta del primer administrador al iniciar la aplicación, a partir de la sección
/// "InitialAdministrator" de la configuración (User Secrets o variables de entorno; nunca appsettings.json).
/// Solo actúa si todavía no existe ningún administrador.
/// </summary>
public static class InitialAdministratorSetup
{
    public static async Task EnsureInitialAdministratorAsync(this WebApplication app)
    {
        var section = app.Configuration.GetSection("InitialAdministrator");
        var data = new InitialAdministratorDto
        {
            UserName = section["UserName"],
            Email = section["Email"],
            Password = section["Password"]
        };

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SIGID.InitialAdministrator");
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        try
        {
            var result = await authService.EnsureInitialAdministratorAsync(data);

            // No se registra la contraseña ni el hash, solo el resultado.
            switch (result)
            {
                case InitialAdministratorResult.Created:
                    logger.LogInformation("Se creó la cuenta inicial del administrador '{UserName}'.", data.UserName?.Trim());
                    break;
                case InitialAdministratorResult.NotConfigured:
                    logger.LogWarning("No hay administradores registrados. Configure la sección InitialAdministrator (ver README) para crear la cuenta inicial.");
                    break;
            }
        }
        catch (BusinessException ex)
        {
            logger.LogError("No se creó la cuenta inicial: {Reason}", ex.Message);
        }
        catch (Exception ex)
        {
            // Normalmente: la base de datos todavía no existe o falta aplicar la migración del Sprint 3.
            logger.LogWarning("No se pudo comprobar la cuenta inicial del administrador ({Error}). Aplique las migraciones y reinicie.", ex.GetType().Name);
        }
    }
}
