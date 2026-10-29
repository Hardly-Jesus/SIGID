using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SIGID.Application.Interfaces.Services;
using SIGID.Application.Services.Authentication;
using SIGID.Application.Services.Categories;
using SIGID.Application.Services.Dashboard;
using SIGID.Application.Services.Inventory;
using SIGID.Application.Services.Products;
using SIGID.Application.Services.Sales;
using SIGID.Application.Services.Suppliers;
using SIGID.Infrastructure;
using SIGID.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// MVC
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------
// Capa Infrastructure: DbContext (SQL Server) + repositorios
// ---------------------------------------------------------------
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------------------------------------------------------
// Capa Application: servicios
// ---------------------------------------------------------------
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ISaleService, SaleService>();

// ---------------------------------------------------------------
// Autenticación del administrador (cookies) — HU-11 / HU-12
// ---------------------------------------------------------------
var sessionMinutes = builder.Configuration.GetValue("Authentication:SessionMinutes", 60);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITicketStore, MemorySessionTicketStore>();
builder.Services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, ConfigureTicketStore>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";

        options.Cookie.Name = "SIGID.Auth";
        options.Cookie.HttpOnly = true;   // no accesible desde JavaScript
        options.Cookie.SameSite = SameSiteMode.Lax;
        // En producción la cookie solo viaja por HTTPS.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        // La sesión vence tras el tiempo configurado sin actividad.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionMinutes);
        options.SlidingExpiration = true;

        // En cada solicitud se comprueba que el administrador siga activo.
        options.Events.OnValidatePrincipal = AdministratorCookieEvents.ValidatePrincipalAsync;
    });

// ---------------------------------------------------------------
// Autorización — HU-13: todo requiere sesión, salvo lo marcado con [AllowAnonymous]
// ---------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Crea el primer administrador si está configurado y todavía no existe ninguno.
await app.EnsureInitialAdministratorAsync();

app.Run();
