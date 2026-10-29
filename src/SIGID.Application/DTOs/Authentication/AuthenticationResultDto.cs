namespace SIGID.Application.DTOs.Authentication;

/// <summary>
/// Resultado de la autenticación. Nunca incluye la contraseña ni el hash.
/// </summary>
public class AuthenticationResultDto
{
    public bool IsAuthenticated { get; init; }

    public int AdministratorId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public static AuthenticationResultDto Success(int administratorId, string userName) => new()
    {
        IsAuthenticated = true,
        AdministratorId = administratorId,
        UserName = userName
    };

    public static AuthenticationResultDto Failure(string message) => new()
    {
        IsAuthenticated = false,
        ErrorMessage = message
    };
}
