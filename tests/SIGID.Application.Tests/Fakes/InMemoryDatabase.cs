using SIGID.Domain.Entities;

namespace SIGID.Application.Tests.Fakes;

/// <summary>
/// "Base de datos" en memoria para probar los servicios sin SQL Server.
/// </summary>
public class InMemoryDatabase
{
    public object Sync { get; } = new();

    /// <summary>
    /// Las transacciones simuladas se ejecutan de a una, para que el rollback de una
    /// no deshaga lo que confirmó otra (en SQL Server cada transacción está aislada).
    /// </summary>
    public SemaphoreSlim TransactionLock { get; } = new(1, 1);

    public List<Category> Categories { get; } = new();

    public List<Supplier> Suppliers { get; } = new();

    public List<Product> Products { get; } = new();

    public List<InventoryMovement> Movements { get; } = new();

    public List<Administrator> Administrators { get; } = new();

    public List<Sale> Sales { get; } = new();

    private int _nextId = 1;

    public int NextId() => Interlocked.Increment(ref _nextId);

    /// <summary>
    /// Completa las propiedades de navegación como lo haría Include() en EF Core.
    /// </summary>
    public void FillNavigation(Product product)
    {
        product.Category = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        product.Supplier = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
    }

    // Ayudas para preparar datos en las pruebas
    public Category AddCategory(string name, bool isActive = true)
    {
        var category = new Category { Id = NextId(), Name = name, IsActive = isActive };
        Categories.Add(category);
        return category;
    }

    public Supplier AddSupplier(string name, string? identification = null, bool isActive = true)
    {
        var supplier = new Supplier { Id = NextId(), Name = name, Identification = identification, IsActive = isActive };
        Suppliers.Add(supplier);
        return supplier;
    }

    public Product AddProduct(string sku, Category category, Supplier supplier, int currentStock = 0, int minimumStock = 0, bool isActive = true)
    {
        var product = new Product
        {
            Id = NextId(),
            SKU = sku,
            Name = "Producto " + sku,
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            UnitOfMeasure = "UND",
            MinimumStock = minimumStock,
            MaximumStock = Math.Max(minimumStock, 100),
            CurrentStock = currentStock,
            IsActive = isActive
        };
        Products.Add(product);
        FillNavigation(product);
        return product;
    }
}
