namespace SIGID.Application.DTOs.Authentication;

/// <summary>
/// Credenciales que envía el administrador para iniciar sesión.
/// </summary>
public class LoginDto
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
