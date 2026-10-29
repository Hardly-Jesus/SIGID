namespace SIGID.Application.DTOs.Authentication;

/// <summary>
/// Datos de la cuenta inicial del administrador. Se leen de la configuración externa
/// (secretos de desarrollo o variables de entorno), nunca del código fuente.
/// </summary>
public class InitialAdministratorDto
{
    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }
}
