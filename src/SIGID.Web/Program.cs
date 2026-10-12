using SIGID.Application.Interfaces.Services;
using SIGID.Application.Services.Categories;
using SIGID.Application.Services.Dashboard;
using SIGID.Application.Services.Inventory;
using SIGID.Application.Services.Products;
using SIGID.Application.Services.Suppliers;
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
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

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
