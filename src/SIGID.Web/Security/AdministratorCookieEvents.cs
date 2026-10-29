using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AdminAuthenticationService = SIGID.Application.Interfaces.Services.IAuthenticationService;

namespace SIGID.Web.Security;

/// <summary>
/// Eventos de la cookie de autenticación.
/// </summary>
public static class AdministratorCookieEvents
{
    /// <summary>
    /// En cada solicitud comprueba que el administrador de la sesión siga existiendo y activo.
    /// Si no, la sesión se descarta y se vuelve a pedir el inicio de sesión.
    /// </summary>
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var authService = context.HttpContext.RequestServices.GetRequiredService<AdminAuthenticationService>();

        if (!int.TryParse(id, out var administratorId) || !await authService.IsAdministratorActiveAsync(administratorId))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
