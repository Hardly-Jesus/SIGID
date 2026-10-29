namespace SIGID.Domain.Entities;

/// <summary>
/// Administrador con acceso a SIGID. Es el único tipo de usuario del sistema.
/// </summary>
public class Administrator
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña generado con PasswordHasher. Nunca se guarda la contraseña original.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
