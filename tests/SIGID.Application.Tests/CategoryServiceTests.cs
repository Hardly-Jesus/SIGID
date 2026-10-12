using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Categories;
using SIGID.Application.Tests.Fakes;

namespace SIGID.Application.Tests;

// HU-06 / TASK 6.8 — validación del CRUD de categorías
public class CategoryServiceTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(new FakeCategoryRepository(_db));
    }

    [Fact]
    public async Task Create_ValidCategory_IsStored()
    {
        var id = await _service.CreateAsync(new CategoryDto { Name = "  Limpieza ", Description = " ", IsActive = true });

        var stored = await _service.GetByIdAsync(id);
        Assert.Equal("Limpieza", stored!.Name);
        Assert.Null(stored.Description);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task Create_WithoutName_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(new CategoryDto { Name = "" }));
        Assert.Equal(nameof(CategoryDto.Name), ex.PropertyName);
        Assert.Empty(_db.Categories);
    }

    [Fact]
    public async Task Create_WithExistingName_IsRejected()
    {
        _db.AddCategory("Limpieza");

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(new CategoryDto { Name = "LIMPIEZA" }));
        Assert.Equal(nameof(CategoryDto.Name), ex.PropertyName);
        Assert.Single(_db.Categories);
    }

    [Fact]
    public async Task Update_ExistingCategory_SavesChanges()
    {
        var category = _db.AddCategory("Limpieza");

        await _service.UpdateAsync(new CategoryDto { Id = category.Id, Name = "Aseo", Description = "Hogar", IsActive = false });

        var stored = await _service.GetByIdAsync(category.Id);
        Assert.Equal("Aseo", stored!.Name);
        Assert.Equal("Hogar", stored.Description);
        Assert.False(stored.IsActive);
    }

    [Fact]
    public async Task Update_SameNameDifferentCase_IsAllowed()
    {
        var category = _db.AddCategory("Limpieza");

        await _service.UpdateAsync(new CategoryDto { Id = category.Id, Name = "limpieza" });

        Assert.Equal("limpieza", category.Name);
    }

    [Fact]
    public async Task Update_WithNameOfAnotherCategory_IsRejected()
    {
        _db.AddCategory("Limpieza");
        var other = _db.AddCategory("Bebidas");

        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.UpdateAsync(new CategoryDto { Id = other.Id, Name = "Limpieza" }));
        Assert.Equal("Bebidas", other.Name);
    }

    [Fact]
    public async Task Delete_CategoryWithoutProducts_IsRemoved()
    {
        var category = _db.AddCategory("Temporal");

        await _service.DeleteAsync(category.Id);

        Assert.Empty(_db.Categories);
    }

    [Fact]
    public async Task Delete_CategoryWithProducts_IsBlocked()
    {
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _db.AddProduct("BEB-001", category, supplier);

        await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteAsync(category.Id));
        Assert.Single(_db.Categories);
    }

    [Fact]
    public async Task GetAll_IncludesProductCount()
    {
        var category = _db.AddCategory("Bebidas");
        _db.AddCategory("Vacía");
        var supplier = _db.AddSupplier("Proveedor");
        _db.AddProduct("BEB-001", category, supplier);
        _db.AddProduct("BEB-002", category, supplier);

        var all = await _service.GetAllAsync();

        Assert.Equal(2, all.Single(c => c.Name == "Bebidas").ProductCount);
        Assert.Equal(0, all.Single(c => c.Name == "Vacía").ProductCount);
    }
}
