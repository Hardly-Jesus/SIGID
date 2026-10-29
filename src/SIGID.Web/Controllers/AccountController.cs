using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Web.ViewModels;
using AdminAuthenticationService = SIGID.Application.Interfaces.Services.IAuthenticationService;

namespace SIGID.Web.Controllers;

/// <summary>
/// Inicio y cierre de sesión del administrador (HU-12).
/// La verificación de credenciales la hace IAuthenticationService.
/// </summary>
public class AccountController : Controller
{
    private readonly AdminAuthenticationService _authenticationService;

    public AccountController(AdminAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // GET: /Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya hay una sesión activa no tiene sentido mostrar el formulario.
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return LoginForm(model);

        var result = await _authenticationService.AuthenticateAsync(model.ToDto(), cancellationToken);

        if (!result.IsAuthenticated)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Usuario o contraseña incorrectos.");
            return LoginForm(model);
        }

        // Solo se guarda lo necesario para identificar al administrador.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.AdministratorId.ToString()),
            new(ClaimTypes.Name, result.UserName ?? string.Empty)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Se descarta cualquier sesión anterior antes de crear la nueva.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

        return RedirectToLocal(model.ReturnUrl);
    }

    // POST: /Account/Logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Borra la cookie y elimina la sesión guardada en el servidor.
        // No modifica los datos del administrador.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Sesión cerrada correctamente.";
        return RedirectToAction(nameof(Login));
    }

    // La contraseña nunca se devuelve a la vista (el campo password se dibuja siempre vacío).
    private ViewResult LoginForm(LoginViewModel model)
    {
        model.Password = null;
        return View(nameof(Login), model);
    }

    // Evita redirecciones abiertas: solo se aceptan rutas locales.
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }
}
