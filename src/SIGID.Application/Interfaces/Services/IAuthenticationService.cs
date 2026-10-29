using SIGID.Application.DTOs.Authentication;

namespace SIGID.Application.Interfaces.Services;

public interface IAuthenticationService
{
    /// <summary>
    /// Valida las credenciales del administrador. Si fallan, el mensaje es siempre el mismo
    /// para no revelar si el usuario existe o cuál dato es incorrecto.
    /// </summary>
    Task<AuthenticationResultDto> AuthenticateAsync(LoginDto login, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si el administrador sigue existiendo y activo (se usa al validar la sesión).
    /// </summary>
    Task<bool> IsAdministratorActiveAsync(int administratorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea la cuenta inicial solo si todavía no existe ningún administrador.
    /// </summary>
    Task<InitialAdministratorResult> EnsureInitialAdministratorAsync(InitialAdministratorDto data, CancellationToken cancellationToken = default);
}
