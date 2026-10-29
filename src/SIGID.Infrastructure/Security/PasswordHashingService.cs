using Microsoft.AspNetCore.Identity;
using SIGID.Application.Interfaces.Security;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Security;

/// <summary>
/// Usa el PasswordHasher oficial de ASP.NET Core Identity (PBKDF2 con sal aleatoria).
/// </summary>
public class PasswordHashingService : IPasswordHashingService
{
    private readonly PasswordHasher<Administrator> _hasher = new();

    // El hasher no usa los datos del usuario; se pasa una instancia vacía.
    private static readonly Administrator HashUser = new();

    public string HashPassword(string password)
        => _hasher.HashPassword(HashUser, password);

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        try
        {
            var result = _hasher.VerifyHashedPassword(HashUser, passwordHash, providedPassword);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Un hash dañado se trata como contraseña incorrecta.
            return false;
        }
    }
}
