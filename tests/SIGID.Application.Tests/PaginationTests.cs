using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Sales;
using SIGID.Application.Services.Inventory;
using SIGID.Application.Services.Sales;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests;

/// <summary>
/// Historiales por páginas (optimización de rendimiento): mismas reglas, menos registros por consulta.
/// </summary>
public class PaginationTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakeInventoryMovementRepository _movements;
    private readonly InventoryService _inventory;
    private readonly SaleService _sales;
    private readonly Product _product;
    private readonly Product _other;

    public PaginationTests()
    {
        _movements = new FakeInventoryMovementRepository(_db);
        var products = new FakeProductRepository(_db);
        _inventory = new InventoryService(_movements, products);
        _sales = new SaleService(new FakeSaleRepository(_db), products, _inventory, new FakeTransactionRunner(_db));
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("A", category, supplier, currentStock: 1000);
        _other = _db.AddProduct("B", category, supplier, currentStock: 1000);
    }

    private void AddMovements(Product product, MovementType type, int count, int quantity)
    {
        for (var i = 0; i < count; i++)
            _db.Movements.Add(new InventoryMovement
            {
                Id = _db.NextId(), ProductId = product.Id, MovementType = type, Quantity = quantity,
                Date = new DateTime(2026, 1, 1).AddHours(i), StockAfter = 0
            });
    }

    [Fact]
    public async Task MovementHistory_ReturnsOnePage_AndTotalsOfAllPages()
    {
        AddMovements(_product, MovementType.Entry, 70, 3);
        AddMovements(_product, MovementType.Exit, 40, 2);
        AddMovements(_other, MovementType.Entry, 30, 9);

        var page2 = await _inventory.GetHistoryPageAsync(new MovementFilterDto { ProductId = _product.Id }, page: 2, pageSize: 50);

        Assert.Equal(110, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.Equal(50, page2.Items.Count);
        Assert.Equal(210, page2.TotalEntries);   // 70 × 3, aunque no estén en esta página
        Assert.Equal(80, page2.TotalExits);      // 40 × 2
        Assert.All(page2.Items, m => Assert.Equal(_product.Id, m.ProductId));
    }

    [Fact]
    public async Task MovementHistory_PagesDoNotRepeatOrLoseRecords()
    {
        AddMovements(_product, MovementType.Entry, 120, 1);
        var filter = new MovementFilterDto();

        var ids = new List<int>();
        for (var page = 1; page <= 3; page++)
            ids.AddRange((await _inventory.GetHistoryPageAsync(filter, page, 50)).Items.Select(m => m.Id));

        Assert.Equal(120, ids.Count);
        Assert.Equal(120, ids.Distinct().Count());
        var all = await _inventory.GetHistoryAsync(filter);
        Assert.Equal(all.Select(m => m.Id), ids);   // mismo orden que el historial completo
    }

    [Fact]
    public async Task MovementHistory_PageOutOfRange_ShowsLastPage()
    {
        AddMovements(_product, MovementType.Entry, 60, 1);

        var result = await _inventory.GetHistoryPageAsync(new MovementFilterDto(), page: 99, pageSize: 50);

        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task MovementHistory_InvalidDates_StillRejected()
    {
        await Assert.ThrowsAsync<SIGID.Application.Exceptions.BusinessException>(() =>
            _inventory.GetHistoryPageAsync(new MovementFilterDto { FromDate = new DateTime(2026, 2, 1), ToDate = new DateTime(2026, 1, 1) }, 1, 50));
    }

    [Fact]
    public async Task EntriesPage_OnlyIncludesEntries()
    {
        AddMovements(_product, MovementType.Entry, 55, 1);
        AddMovements(_product, MovementType.Exit, 10, 1);

        var result = await _inventory.GetMovementsPageAsync(MovementType.Entry, 2, 50);

        Assert.Equal(55, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.All(result.Items, m => Assert.Equal(MovementType.Entry, m.MovementType));
    }

    [Fact]
    public async Task SaleHistory_ReturnsPagesNewestFirst()
    {
        for (var i = 0; i < 75; i++)
            _db.AddSale(new DateTime(2026, 3, 1).AddDays(i), _product, 1);

        var first = await _sales.GetHistoryPageAsync(new SaleHistoryFilterDto(), 1, 50);
        var second = await _sales.GetHistoryPageAsync(new SaleHistoryFilterDto(), 2, 50);

        Assert.Equal(75, first.TotalCount);
        Assert.Equal(50, first.Items.Count);
        Assert.Equal(25, second.Items.Count);
        Assert.Equal(new DateTime(2026, 3, 1).AddDays(74), first.Items[0].Date);
        Assert.True(first.Items[^1].Date > second.Items[0].Date);
        Assert.Equal(1, first.Items[0].TotalQuantity);
    }
}
