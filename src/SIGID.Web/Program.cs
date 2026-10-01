using SIGID.Application.Interfaces.Services;
using SIGID.Application.Services.Products;
using SIGID.Infrastructure;

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
