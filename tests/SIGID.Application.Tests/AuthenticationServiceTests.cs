using SIGID.Application.DTOs.Authentication;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Authentication;
using SIGID.Application.Tests.Fakes;

namespace SIGID.Application.Tests;

// HU-11 (TASK 11.3, 11.5, 11.8 y 11.9)
public class AuthenticationServiceTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _service = new AuthenticationService(new FakeAdministratorRepository(_db), _hasher);
    }

    private async Task CreateAdminAsync(string password = "Clave-Segura-1")
    {
        await _service.EnsureInitialAdministratorAsync(new InitialAdministratorDto
        {
            UserName = "admin",
            Email = "Admin@Sigid.Local",
            Password = password
        });
    }

    [Fact]
    public async Task InitialAdministrator_IsCreatedWithHashAndNormalizedEmail()
    {
        var result = await _service.EnsureInitialAdministratorAsync(new InitialAdministratorDto
        {
            UserName = " admin ",
            Email = "Admin@Sigid.Local",
            Password = "Clave-Segura-1"
        });

        Assert.Equal(InitialAdministratorResult.Created, result);
        var admin = Assert.Single(_db.Administrators);
        Assert.Equal("admin", admin.UserName);
        Assert.Equal("admin@sigid.local", admin.Email);
        Assert.True(admin.IsActive);
        Assert.NotEqual("Clave-Segura-1", admin.PasswordHash);
        Assert.DoesNotContain("Clave-Segura-1", admin.PasswordHash);
    }

    [Fact]
    public async Task InitialAdministrator_IsNotDuplicated()
    {
        await CreateAdminAsync();

        var result = await _service.EnsureInitialAdministratorAsync(new InitialAdministratorDto
        {
            UserName = "otro",
            Email = "otro@sigid.local",
            Password = "Otra-Clave-2"
        });

        Assert.Equal(InitialAdministratorResult.AlreadyExists, result);
        Assert.Single(_db.Administrators);
    }

    [Fact]
    public async Task InitialAdministrator_WithoutConfiguration_IsNotCreated()
    {
        var result = await _service.EnsureInitialAdministratorAsync(new InitialAdministratorDto());

        Assert.Equal(InitialAdministratorResult.NotConfigured, result);
        Assert.Empty(_db.Administrators);
    }

    [Theory]
    [InlineData("admin", "correo-invalido", "Clave-Segura-1")]
    [InlineData("admin", "admin@sigid.local", "corta")]
    public async Task InitialAdministrator_InvalidData_IsRejected(string user, string email, string password)
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.EnsureInitialAdministratorAsync(
            new InitialAdministratorDto { UserName = user, Email = email, Password = password }));
        Assert.Empty(_db.Administrators);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("admin@sigid.local")]
    [InlineData("  Admin@SIGID.local ")]
    public async Task Authenticate_ValidCredentials_ByUserNameOrEmail(string login)
    {
        await CreateAdminAsync();

        var result = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = login, Password = "Clave-Segura-1" });

        Assert.True(result.IsAuthenticated);
        Assert.Equal("admin", result.UserName);
        Assert.Equal(_db.Administrators[0].Id, result.AdministratorId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_And_UnknownUser_GetSameGenericMessage()
    {
        await CreateAdminAsync();

        var wrongPassword = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = "admin", Password = "incorrecta" });
        var unknownUser = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = "nadie", Password = "Clave-Segura-1" });

        Assert.False(wrongPassword.IsAuthenticated);
        Assert.False(unknownUser.IsAuthenticated);
        Assert.Equal(AuthenticationService.InvalidCredentialsMessage, wrongPassword.ErrorMessage);
        Assert.Equal(wrongPassword.ErrorMessage, unknownUser.ErrorMessage);
        Assert.Equal(0, unknownUser.AdministratorId);
    }

    [Fact]
    public async Task Authenticate_UnknownUser_StillVerifiesAHash()
    {
        // Así la respuesta tarda lo mismo exista o no la cuenta.
        await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = "nadie", Password = "x" });

        Assert.Equal(1, _hasher.Verifications);
    }

    [Fact]
    public async Task Authenticate_PasswordIsCaseSensitive()
    {
        await CreateAdminAsync();

        var result = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = "admin", Password = "clave-segura-1" });

        Assert.False(result.IsAuthenticated);
    }

    [Fact]
    public async Task Authenticate_InactiveAdministrator_IsRejectedWithGenericMessage()
    {
        await CreateAdminAsync();
        _db.Administrators[0].IsActive = false;

        var result = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = "admin", Password = "Clave-Segura-1" });

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationService.InvalidCredentialsMessage, result.ErrorMessage);
        Assert.False(await _service.IsAdministratorActiveAsync(_db.Administrators[0].Id));
    }

    [Theory]
    [InlineData("", "Clave-Segura-1")]
    [InlineData("   ", "Clave-Segura-1")]
    [InlineData("admin", "")]
    public async Task Authenticate_EmptyFields_AreRejected(string login, string password)
    {
        await CreateAdminAsync();

        var result = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = login, Password = password });

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationService.MissingCredentialsMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task Authenticate_Result_NeverContainsPasswordOrHash()
    {
        await CreateAdminAsync();
        var hash = _db.Administrators[0].PasswordHash;

        var result = await _service.AuthenticateAsync(new LoginDto { UserNameOrEmail = "admin", Password = "Clave-Segura-1" });

        var values = typeof(AuthenticationResultDto).GetProperties().Select(p => p.GetValue(result)?.ToString() ?? "");
        Assert.DoesNotContain(values, v => v.Contains("Clave-Segura-1") || v.Contains(hash));
    }
}
