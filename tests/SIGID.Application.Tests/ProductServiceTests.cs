using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Products;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;

namespace SIGID.Application.Tests;

// HU-05 / TASK 5.7 — validación del CRUD de productos
public class ProductServiceTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly ProductService _service;
    private readonly Category _category;
    private readonly Supplier _supplier;

    public ProductServiceTests()
    {
        _service = new ProductService(
            new FakeProductRepository(_db),
            new FakeCategoryRepository(_db),
            new FakeSupplierRepository(_db));

        _category = _db.AddCategory("Bebidas");
        _supplier = _db.AddSupplier("Distribuidora Andina", "1020304050");
    }

    private ProductDto ValidProduct(string sku = "BEB-001") => new()
    {
        SKU = sku,
        Name = "Agua mineral 500 ml",
        Description = "Botella",
        CategoryId = _category.Id,
        SupplierId = _supplier.Id,
        UnitOfMeasure = "und",
        MinimumStock = 10,
        MaximumStock = 200,
        IsActive = true
    };

    [Fact]
    public async Task Create_ValidProduct_IsStoredWithZeroStock()
    {
        var id = await _service.CreateAsync(ValidProduct(" beb-001 "));

        var stored = await _service.GetByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Equal("BEB-001", stored!.SKU);
        Assert.Equal("UND", stored.UnitOfMeasure);
        Assert.Equal(0, stored.CurrentStock);
        Assert.Equal("Bebidas", stored.CategoryName);
        Assert.Equal("Distribuidora Andina", stored.SupplierName);
    }

    [Fact]
    public async Task Create_WithoutSku_IsRejected()
    {
        var dto = ValidProduct();
        dto.SKU = "  ";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.SKU), ex.PropertyName);
        Assert.Empty(_db.Products);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_IsRejected()
    {
        await _service.CreateAsync(ValidProduct("BEB-001"));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(ValidProduct("beb-001")));
        Assert.Equal(nameof(ProductDto.SKU), ex.PropertyName);
        Assert.Single(_db.Products);
    }

    [Fact]
    public async Task Create_WithoutName_IsRejected()
    {
        var dto = ValidProduct();
        dto.Name = "";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.Name), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithoutCategory_IsRejected()
    {
        var dto = ValidProduct();
        dto.CategoryId = 0;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.CategoryId), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithNonexistentCategory_IsRejected()
    {
        var dto = ValidProduct();
        dto.CategoryId = 9999;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.CategoryId), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithInactiveCategory_IsRejected()
    {
        var inactive = _db.AddCategory("Descontinuados", isActive: false);
        var dto = ValidProduct();
        dto.CategoryId = inactive.Id;

        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_WithoutSupplier_IsRejected()
    {
        var dto = ValidProduct();
        dto.SupplierId = 0;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.SupplierId), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithoutUnitOfMeasure_IsRejected()
    {
        var dto = ValidProduct();
        dto.UnitOfMeasure = "";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.UnitOfMeasure), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithNegativeMinimumStock_IsRejected()
    {
        var dto = ValidProduct();
        dto.MinimumStock = -1;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.MinimumStock), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithMaximumLowerThanMinimum_IsRejected()
    {
        var dto = ValidProduct();
        dto.MinimumStock = 50;
        dto.MaximumStock = 20;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(ProductDto.MaximumStock), ex.PropertyName);
    }

    [Fact]
    public async Task Update_ExistingProduct_SavesChangesAndKeepsStock()
    {
        var product = _db.AddProduct("BEB-010", _category, _supplier, currentStock: 35);
        var other = _db.AddCategory("Lácteos");

        var dto = ValidProduct("BEB-010");
        dto.Id = product.Id;
        dto.Name = "Agua con gas";
        dto.CategoryId = other.Id;
        dto.IsActive = false;
        dto.CurrentStock = 999; // no debe modificarse desde el formulario

        await _service.UpdateAsync(dto);

        var stored = await _service.GetByIdAsync(product.Id);
        Assert.Equal("Agua con gas", stored!.Name);
        Assert.Equal("Lácteos", stored.CategoryName);
        Assert.False(stored.IsActive);
        Assert.Equal(35, stored.CurrentStock);
    }

    [Fact]
    public async Task Update_WithSkuOfAnotherProduct_IsRejected()
    {
        _db.AddProduct("BEB-001", _category, _supplier);
        var second = _db.AddProduct("BEB-002", _category, _supplier);

        var dto = ValidProduct("BEB-001");
        dto.Id = second.Id;

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(dto));
        Assert.Equal("BEB-002", second.SKU);
    }

    [Fact]
    public async Task Update_KeepingAnInactiveCategory_IsAllowed()
    {
        var product = _db.AddProduct("BEB-020", _category, _supplier);
        _category.IsActive = false;

        var dto = ValidProduct("BEB-020");
        dto.Id = product.Id;
        dto.Name = "Nombre nuevo";

        await _service.UpdateAsync(dto);

        Assert.Equal("Nombre nuevo", product.Name);
    }

    [Fact]
    public async Task GetAll_ReturnsRegisteredProducts()
    {
        await _service.CreateAsync(ValidProduct("BEB-001"));
        await _service.CreateAsync(ValidProduct("BEB-002"));

        var all = await _service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }
}
