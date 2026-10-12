using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Inventory;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests;

// HU-08 (TASK 8.9), HU-09 (TASK 9.9) y HU-10 (TASK 10.7)
public class InventoryServiceTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakeInventoryMovementRepository _movements;
    private readonly InventoryService _service;
    private readonly Product _product;

    public InventoryServiceTests()
    {
        _movements = new FakeInventoryMovementRepository(_db);
        _service = new InventoryService(_movements, new FakeProductRepository(_db));

        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("BEB-001", category, supplier, currentStock: 0, minimumStock: 5);
    }

    private RegisterMovementDto Movement(int quantity, DateTime? date = null, int? productId = null) => new()
    {
        ProductId = productId ?? _product.Id,
        Quantity = quantity,
        Date = date ?? DateTime.Now.AddMinutes(-1),
        Description = "Prueba"
    };

    // ---------------- Entradas (HU-08) ----------------

    [Fact]
    public async Task Entry_Valid_IncreasesStockAndRegistersMovement()
    {
        var result = await _service.RegisterEntryAsync(Movement(20));

        Assert.Equal(20, _product.CurrentStock);
        var movement = Assert.Single(_db.Movements);
        Assert.Equal(MovementType.Entry, movement.MovementType);
        Assert.Equal(20, movement.Quantity);
        Assert.Equal(20, movement.StockAfter);
        Assert.Equal("Prueba", movement.Description);
        Assert.Equal("BEB-001", result.ProductSku);
    }

    [Fact]
    public async Task Entry_IsAddedToExistingStock()
    {
        await _service.RegisterEntryAsync(Movement(20));
        await _service.RegisterEntryAsync(Movement(15));

        Assert.Equal(35, _product.CurrentStock);
        Assert.Equal(35, _db.Movements.Last().StockAfter);
    }

    [Fact]
    public async Task Entry_WithoutProduct_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(Movement(5, productId: 0)));
        Assert.Equal(nameof(RegisterMovementDto.ProductId), ex.PropertyName);
        Assert.Empty(_db.Movements);
    }

    [Fact]
    public async Task Entry_ForNonexistentProduct_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(Movement(5, productId: 9999)));
        Assert.Equal(nameof(RegisterMovementDto.ProductId), ex.PropertyName);
        Assert.Empty(_db.Movements);
    }

    [Fact]
    public async Task Entry_WithZeroQuantity_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(Movement(0)));
        Assert.Equal(nameof(RegisterMovementDto.Quantity), ex.PropertyName);
        Assert.Equal(0, _product.CurrentStock);
    }

    [Fact]
    public async Task Entry_WithNegativeQuantity_IsRejected()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(Movement(-3)));
        Assert.Equal(0, _product.CurrentStock);
        Assert.Empty(_db.Movements);
    }

    [Fact]
    public async Task Entry_ForInactiveProduct_IsRejected()
    {
        _product.IsActive = false;

        await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(Movement(5)));
        Assert.Equal(0, _product.CurrentStock);
    }

    [Fact]
    public async Task Entry_WithFutureDate_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(Movement(5, DateTime.Now.AddDays(2))));
        Assert.Equal(nameof(RegisterMovementDto.Date), ex.PropertyName);
    }

    [Fact]
    public async Task Entry_WithoutDate_IsRejected()
    {
        var dto = Movement(5);
        dto.Date = default;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterEntryAsync(dto));
        Assert.Equal(nameof(RegisterMovementDto.Date), ex.PropertyName);
    }

    // ---------------- Salidas (HU-09) ----------------

    [Fact]
    public async Task Exit_Valid_DecreasesStockAndRegistersMovement()
    {
        await _service.RegisterEntryAsync(Movement(20));

        await _service.RegisterExitAsync(Movement(8));

        Assert.Equal(12, _product.CurrentStock);
        var exit = _db.Movements.Last();
        Assert.Equal(MovementType.Exit, exit.MovementType);
        Assert.Equal(8, exit.Quantity);
        Assert.Equal(12, exit.StockAfter);
    }

    [Fact]
    public async Task Exit_GreaterThanStock_IsRejectedAndNothingChanges()
    {
        await _service.RegisterEntryAsync(Movement(10));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterExitAsync(Movement(11)));

        Assert.Equal(nameof(RegisterMovementDto.Quantity), ex.PropertyName);
        Assert.Contains("Disponible: 10", ex.Message);
        Assert.Equal(10, _product.CurrentStock);
        Assert.Single(_db.Movements);
    }

    [Fact]
    public async Task Exit_OfAllStock_LeavesZeroNotNegative()
    {
        await _service.RegisterEntryAsync(Movement(10));

        await _service.RegisterExitAsync(Movement(10));

        Assert.Equal(0, _product.CurrentStock);
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterExitAsync(Movement(1)));
        Assert.Equal(0, _product.CurrentStock);
    }

    [Fact]
    public async Task Exit_WithZeroQuantity_IsRejected()
    {
        await _service.RegisterEntryAsync(Movement(10));

        await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterExitAsync(Movement(0)));
        Assert.Equal(10, _product.CurrentStock);
    }

    [Fact]
    public async Task Exit_WithoutProduct_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterExitAsync(Movement(1, productId: 0)));
        Assert.Equal(nameof(RegisterMovementDto.ProductId), ex.PropertyName);
    }

    [Fact]
    public async Task Exit_ForNonexistentProduct_IsRejected()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterExitAsync(Movement(1, productId: 9999)));
    }

    [Fact]
    public async Task Exit_WhenStockChangedConcurrently_IsRejectedWithoutMovement()
    {
        await _service.RegisterEntryAsync(Movement(10));
        _movements.SimulateConcurrentExit = true;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.RegisterExitAsync(Movement(5)));

        Assert.Contains("Existencias insuficientes", ex.Message);
        Assert.Single(_db.Movements);
    }

    // ---------------- Historial (HU-10) ----------------

    [Fact]
    public async Task History_IsOrderedFromNewestToOldest()
    {
        await _service.RegisterEntryAsync(Movement(10, DateTime.Today.AddDays(-3)));
        await _service.RegisterEntryAsync(Movement(5, DateTime.Today.AddDays(-1)));
        await _service.RegisterExitAsync(Movement(2, DateTime.Today.AddDays(-2)));

        var history = await _service.GetHistoryAsync(new MovementFilterDto());

        Assert.Equal(3, history.Count);
        Assert.True(history[0].Date > history[1].Date);
        Assert.True(history[1].Date > history[2].Date);
    }

    [Fact]
    public async Task History_FiltersByProduct()
    {
        var other = _db.AddProduct("BEB-002", _db.Categories[0], _db.Suppliers[0]);
        await _service.RegisterEntryAsync(Movement(10));
        await _service.RegisterEntryAsync(Movement(3, productId: other.Id));

        var history = await _service.GetHistoryAsync(new MovementFilterDto { ProductId = other.Id });

        var only = Assert.Single(history);
        Assert.Equal("BEB-002", only.ProductSku);
    }

    [Fact]
    public async Task History_FiltersByType()
    {
        await _service.RegisterEntryAsync(Movement(10));
        await _service.RegisterExitAsync(Movement(4));

        var exits = await _service.GetHistoryAsync(new MovementFilterDto { MovementType = MovementType.Exit });
        var entries = await _service.GetEntriesAsync();

        Assert.Equal(MovementType.Exit, Assert.Single(exits).MovementType);
        Assert.Equal(MovementType.Entry, Assert.Single(entries).MovementType);
    }

    [Fact]
    public async Task History_FiltersByDateRange_IncludingWholeDays()
    {
        await _service.RegisterEntryAsync(Movement(1, DateTime.Today.AddDays(-10)));
        await _service.RegisterEntryAsync(Movement(2, DateTime.Today.AddDays(-5).AddHours(23)));
        await _service.RegisterEntryAsync(Movement(3, DateTime.Today.AddDays(-4).AddHours(8)));
        await _service.RegisterEntryAsync(Movement(4, DateTime.Today.AddDays(-1)));

        var history = await _service.GetHistoryAsync(new MovementFilterDto
        {
            FromDate = DateTime.Today.AddDays(-5),
            ToDate = DateTime.Today.AddDays(-4)
        });

        Assert.Equal(new[] { 3, 2 }, history.Select(m => m.Quantity).ToArray());
    }

    [Fact]
    public async Task History_CombinesFilters()
    {
        var other = _db.AddProduct("BEB-002", _db.Categories[0], _db.Suppliers[0]);
        await _service.RegisterEntryAsync(Movement(10, DateTime.Today.AddDays(-2)));
        await _service.RegisterExitAsync(Movement(1, DateTime.Today.AddDays(-2)));
        await _service.RegisterEntryAsync(Movement(7, DateTime.Today.AddDays(-2), other.Id));
        await _service.RegisterEntryAsync(Movement(9, DateTime.Today.AddDays(-9)));

        var history = await _service.GetHistoryAsync(new MovementFilterDto
        {
            ProductId = _product.Id,
            MovementType = MovementType.Entry,
            FromDate = DateTime.Today.AddDays(-3),
            ToDate = DateTime.Today
        });

        Assert.Equal(10, Assert.Single(history).Quantity);
    }

    [Fact]
    public async Task History_WithNoMatches_ReturnsEmptyList()
    {
        await _service.RegisterEntryAsync(Movement(10));

        var history = await _service.GetHistoryAsync(new MovementFilterDto { MovementType = MovementType.Exit });

        Assert.Empty(history);
    }

    [Fact]
    public async Task History_WithFromAfterTo_IsRejected()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.GetHistoryAsync(new MovementFilterDto
        {
            FromDate = DateTime.Today,
            ToDate = DateTime.Today.AddDays(-1)
        }));
    }

    [Fact]
    public async Task History_DoesNotModifyStock()
    {
        await _service.RegisterEntryAsync(Movement(10));
        await _service.RegisterExitAsync(Movement(3));

        await _service.GetHistoryAsync(new MovementFilterDto());
        await _service.GetHistoryAsync(new MovementFilterDto { ProductId = _product.Id });

        Assert.Equal(7, _product.CurrentStock);
        Assert.Equal(2, _db.Movements.Count);
    }
}
