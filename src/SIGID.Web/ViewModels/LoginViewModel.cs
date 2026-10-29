using System.ComponentModel.DataAnnotations;
using SIGID.Application.DTOs.Authentication;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Formulario de inicio de sesión (HU-12).
/// </summary>
public class LoginViewModel
{
    [Display(Name = "Usuario o correo electrónico")]
    [Required(ErrorMessage = "Ingrese su usuario o correo electrónico.")]
    [StringLength(150, ErrorMessage = "El usuario o correo no puede superar los 150 caracteres.")]
    public string? UserNameOrEmail { get; set; }

    [Display(Name = "Contraseña")]
    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    /// <summary>
    /// Página a la que se quería entrar antes de iniciar sesión (solo se usa si es local).
    /// </summary>
    public string? ReturnUrl { get; set; }

    public LoginDto ToDto() => new()
    {
        UserNameOrEmail = UserNameOrEmail?.Trim() ?? string.Empty,
        Password = Password ?? string.Empty
    };
}
