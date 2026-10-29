namespace SIGID.Application.Interfaces.Security;

/// <summary>
/// Genera y verifica hashes de contraseñas. Se implementa con PasswordHasher de ASP.NET Core.
/// </summary>
public interface IPasswordHashingService
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string providedPassword);
}
