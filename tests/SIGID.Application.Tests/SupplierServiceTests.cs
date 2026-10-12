using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Suppliers;
using SIGID.Application.Tests.Fakes;

namespace SIGID.Application.Tests;

// HU-07 / TASK 7.8 — validación del CRUD de proveedores
public class SupplierServiceTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly SupplierService _service;

    public SupplierServiceTests()
    {
        _service = new SupplierService(new FakeSupplierRepository(_db));
    }

    private static SupplierDto Valid(string? identification = "131-45678-9") => new()
    {
        Name = "Distribuidora Andina",
        Identification = identification,
        Phone = "+591 2 2456789",
        Email = "Ventas@Andina.com",
        Address = "Av. Arce 123",
        IsActive = true
    };

    [Fact]
    public async Task Create_ValidSupplier_IsStored()
    {
        var id = await _service.CreateAsync(Valid());

        var stored = await _service.GetByIdAsync(id);
        Assert.Equal("Distribuidora Andina", stored!.Name);
        Assert.Equal("ventas@andina.com", stored.Email);
        Assert.Equal("131-45678-9", stored.Identification);
    }

    [Fact]
    public async Task Create_WithoutName_IsRejected()
    {
        var dto = Valid();
        dto.Name = " ";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(SupplierDto.Name), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithExistingIdentification_IsRejected()
    {
        _db.AddSupplier("Otro proveedor", "131-45678-9");

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(Valid("131-45678-9")));
        Assert.Equal(nameof(SupplierDto.Identification), ex.PropertyName);
        Assert.Single(_db.Suppliers);
    }

    [Fact]
    public async Task Create_TwoSuppliersWithoutIdentification_IsAllowed()
    {
        await _service.CreateAsync(Valid(identification: null));
        await _service.CreateAsync(Valid(identification: "  "));

        Assert.Equal(2, _db.Suppliers.Count);
        Assert.All(_db.Suppliers, s => Assert.Null(s.Identification));
    }

    [Fact]
    public async Task Create_WithInvalidEmail_IsRejected()
    {
        var dto = Valid();
        dto.Email = "correo-invalido";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(SupplierDto.Email), ex.PropertyName);
    }

    [Fact]
    public async Task Create_WithInvalidPhone_IsRejected()
    {
        var dto = Valid();
        dto.Phone = "abc";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        Assert.Equal(nameof(SupplierDto.Phone), ex.PropertyName);
    }

    [Fact]
    public async Task Update_ExistingSupplier_SavesChanges()
    {
        var supplier = _db.AddSupplier("Proveedor A", "100");

        var dto = Valid("100");
        dto.Id = supplier.Id;
        dto.Name = "Proveedor A actualizado";
        dto.IsActive = false;

        await _service.UpdateAsync(dto);

        Assert.Equal("Proveedor A actualizado", supplier.Name);
        Assert.False(supplier.IsActive);
    }

    [Fact]
    public async Task Update_WithIdentificationOfAnotherSupplier_IsRejected()
    {
        _db.AddSupplier("Proveedor A", "100");
        var second = _db.AddSupplier("Proveedor B", "200");

        var dto = Valid("100");
        dto.Id = second.Id;

        await Assert.ThrowsAsync<BusinessException>(() => _service.UpdateAsync(dto));
        Assert.Equal("200", second.Identification);
    }

    [Fact]
    public async Task Delete_SupplierWithoutProducts_IsRemoved()
    {
        var supplier = _db.AddSupplier("Temporal");

        await _service.DeleteAsync(supplier.Id);

        Assert.Empty(_db.Suppliers);
    }

    [Fact]
    public async Task Delete_SupplierWithProducts_IsBlocked()
    {
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _db.AddProduct("BEB-001", category, supplier);

        await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteAsync(supplier.Id));
        Assert.Single(_db.Suppliers);
    }
}
