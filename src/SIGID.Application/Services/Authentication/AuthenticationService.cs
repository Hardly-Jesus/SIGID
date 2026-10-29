using System.Text.RegularExpressions;
using SIGID.Application.DTOs.Authentication;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Security;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;

namespace SIGID.Application.Services.Authentication;

/// <summary>
/// Autenticación del administrador (HU-11).
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    // Mismo mensaje para cualquier fallo: no se revela si el usuario existe ni qué dato está mal.
    public const string InvalidCredentialsMessage = "Usuario o contraseña incorrectos.";
    public const string MissingCredentialsMessage = "Ingrese su usuario o correo y su contraseña.";
    public const int MinimumPasswordLength = 8;

    private static readonly Regex EmailFormat = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private readonly IAdministratorRepository _administratorRepository;
    private readonly IPasswordHashingService _passwordHashing;

    // Hash de relleno: se verifica contra él cuando el usuario no existe, para que la
    // respuesta tarde lo mismo y no se pueda adivinar qué cuentas existen.
    private readonly Lazy<string> _dummyHash;

    public AuthenticationService(IAdministratorRepository administratorRepository, IPasswordHashingService passwordHashing)
    {
        _administratorRepository = administratorRepository;
        _passwordHashing = passwordHashing;
        _dummyHash = new Lazy<string>(() => _passwordHashing.HashPassword(Guid.NewGuid().ToString()));
    }

    public async Task<AuthenticationResultDto> AuthenticateAsync(LoginDto login, CancellationToken cancellationToken = default)
    {
        var userNameOrEmail = login.UserNameOrEmail?.Trim() ?? string.Empty;
        var password = login.Password ?? string.Empty;

        if (userNameOrEmail.Length == 0 || password.Length == 0)
            return AuthenticationResultDto.Failure(MissingCredentialsMessage);

        var administrator = await _administratorRepository.GetByUserNameOrEmailAsync(userNameOrEmail, cancellationToken);

        if (administrator is null)
        {
            _passwordHashing.VerifyPassword(_dummyHash.Value, password);
            return AuthenticationResultDto.Failure(InvalidCredentialsMessage);
        }

        var passwordIsValid = _passwordHashing.VerifyPassword(administrator.PasswordHash, password);

        // Una cuenta inactiva recibe el mismo mensaje que una contraseña incorrecta.
        if (!passwordIsValid || !administrator.IsActive)
            return AuthenticationResultDto.Failure(InvalidCredentialsMessage);

        return AuthenticationResultDto.Success(administrator.Id, administrator.UserName);
    }

    public Task<bool> IsAdministratorActiveAsync(int administratorId, CancellationToken cancellationToken = default)
        => _administratorRepository.IsActiveAsync(administratorId, cancellationToken);

    public async Task<InitialAdministratorResult> EnsureInitialAdministratorAsync(InitialAdministratorDto data, CancellationToken cancellationToken = default)
    {
        // Solo se crea si no hay ningún administrador: ejecutarlo varias veces no duplica cuentas.
        if (await _administratorRepository.AnyAsync(cancellationToken))
            return InitialAdministratorResult.AlreadyExists;

        var userName = data.UserName?.Trim() ?? string.Empty;
        var email = data.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = data.Password ?? string.Empty;

        if (userName.Length == 0 || email.Length == 0 || password.Length == 0)
            return InitialAdministratorResult.NotConfigured;

        if (userName.Length > 50)
            throw new BusinessException("El nombre de usuario inicial no puede superar los 50 caracteres.");

        if (email.Length > 150 || !EmailFormat.IsMatch(email))
            throw new BusinessException("El correo del administrador inicial no tiene un formato válido.");

        if (password.Length < MinimumPasswordLength)
            throw new BusinessException($"La contraseña inicial debe tener al menos {MinimumPasswordLength} caracteres.");

        var administrator = new Administrator
        {
            UserName = userName,
            Email = email,
            PasswordHash = _passwordHashing.HashPassword(password),
            IsActive = true
        };

        await _administratorRepository.AddAsync(administrator, cancellationToken);
        return InitialAdministratorResult.Created;
    }
}
