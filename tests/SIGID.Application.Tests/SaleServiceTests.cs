using SIGID.Application.DTOs.Sales;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Inventory;
using SIGID.Application.Services.Sales;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests;

// HU-14 (TASK 14.5, 14.6, 14.10), HU-15 (TASK 15.4 a 15.9) y HU-16 (TASK 16.2, 16.7 a 16.9)
public class SaleServiceTests
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakeInventoryMovementRepository _movements;
    private readonly FakeTransactionRunner _transactions;
    private readonly SaleService _service;
    private readonly Product _cola;
    private readonly Product _agua;
    private readonly Product _inactive;

    public SaleServiceTests()
    {
        _movements = new FakeInventoryMovementRepository(_db);
        _transactions = new FakeTransactionRunner(_db);
        var products = new FakeProductRepository(_db);
        var inventory = new InventoryService(_movements, products);
        _service = new SaleService(new FakeSaleRepository(_db), products, inventory, _transactions);

        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _cola = _db.AddProduct("BEB-001", category, supplier, currentStock: 20);
        _agua = _db.AddProduct("BEB-002", category, supplier, currentStock: 5);
        _inactive = _db.AddProduct("BEB-003", category, supplier, currentStock: 10, isActive: false);
    }

    private static RegisterSaleDto Sale(params (int productId, int quantity)[] lines) => new()
    {
        RequestId = Guid.NewGuid(),
        Details = lines.Select(l => new RegisterSaleDetailDto { ProductId = l.productId, Quantity = l.quantity }).ToList()
    };

    // ---------------- Registro (HU-14 / HU-15) ----------------

    [Fact]
    public async Task Register_SingleProduct_SavesSaleAndDecreasesStock()
    {
        var result = await _service.RegisterAsync(Sale((_cola.Id, 3)));

        Assert.True(result.Succeeded);
        Assert.False(result.WasAlreadyRegistered);
        Assert.Equal(17, _cola.CurrentStock);
        var sale = Assert.Single(_db.Sales);
        var detail = Assert.Single(sale.Details);
        Assert.Equal(3, detail.Quantity);
        Assert.Equal(_cola.Id, detail.ProductId);
        Assert.Equal(17, Assert.Single(result.Sale!.Details).StockAfter);
        Assert.Equal(1, _transactions.Commits);
    }

    [Fact]
    public async Task Register_SeveralProducts_GeneratesOneExitPerProductLinkedToSale()
    {
        var result = await _service.RegisterAsync(Sale((_cola.Id, 4), (_agua.Id, 5)));

        Assert.True(result.Succeeded);
        Assert.Equal(16, _cola.CurrentStock);
        Assert.Equal(0, _agua.CurrentStock);
        Assert.Equal(2, _db.Movements.Count);
        Assert.All(_db.Movements, m =>
        {
            Assert.Equal(MovementType.Exit, m.MovementType);
            Assert.Equal(result.Sale!.Id, m.SaleId);
            Assert.Equal($"Venta #{result.Sale.Id}", m.Description);
        });
        Assert.Equal(16, _db.Movements.Single(m => m.ProductId == _cola.Id).StockAfter);
        Assert.Equal(0, _db.Movements.Single(m => m.ProductId == _agua.Id).StockAfter);
    }

    [Fact]
    public async Task Register_Empty_IsRejected()
    {
        var result = await _service.RegisterAsync(Sale());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("al menos un producto"));
        Assert.Empty(_db.Sales);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Register_QuantityNotPositive_IsRejected(int quantity)
    {
        var result = await _service.RegisterAsync(Sale((_cola.Id, quantity)));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("mayor que cero"));
        Assert.Equal(20, _cola.CurrentStock);
        Assert.Empty(_db.Sales);
    }

    [Fact]
    public async Task Register_UnknownProduct_IsRejected()
    {
        var result = await _service.RegisterAsync(Sale((9999, 1)));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("ya no existe"));
    }

    [Fact]
    public async Task Register_InactiveProduct_IsRejected()
    {
        var result = await _service.RegisterAsync(Sale((_inactive.Id, 1)));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("inactivo"));
        Assert.Equal(10, _inactive.CurrentStock);
    }

    [Fact]
    public async Task Register_DuplicatedProduct_IsRejected()
    {
        var result = await _service.RegisterAsync(Sale((_cola.Id, 1), (_cola.Id, 2)));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("más de una vez"));
        Assert.Empty(_db.Sales);
    }

    [Fact]
    public async Task Register_InsufficientStockInOneProduct_RejectsWholeSale()
    {
        var result = await _service.RegisterAsync(Sale((_cola.Id, 2), (_agua.Id, 6)));

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal(_agua.Id, error.ProductId);
        Assert.True(error.IsStockError);
        Assert.Equal("Pediste 6 UND de «Producto BEB-002», pero solamente hay 5 UND disponibles.", error.Message);
        // Ningún producto cambia y no se guarda nada.
        Assert.Equal(20, _cola.CurrentStock);
        Assert.Equal(5, _agua.CurrentStock);
        Assert.Empty(_db.Sales);
        Assert.Empty(_db.Movements);
    }

    [Fact]
    public async Task Register_ReportsAllProblemsAtOnce()
    {
        var result = await _service.RegisterAsync(Sale((_cola.Id, 50), (_inactive.Id, 1), (_agua.Id, 0)));

        Assert.False(result.Succeeded);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public async Task Register_StockChangesDuringTransaction_RollsBackEverything()
    {
        // La primera salida (cola) se guarda, pero otra operación agota el agua justo antes de su salida.
        _movements.BeforeExit = p => { if (p.Id == _agua.Id) _transactions.ApplyExternalChange(p, 0); };

        var result = await _service.RegisterAsync(Sale((_cola.Id, 2), (_agua.Id, 1)));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.ProductId == _agua.Id && e.IsStockError && e.Message.Contains("ya no tiene existencias"));
        Assert.Equal(1, _transactions.Rollbacks);
        Assert.Equal(20, _cola.CurrentStock);   // la salida de la cola se revirtió
        Assert.Equal(0, _agua.CurrentStock);    // el cambio de la otra operación se conserva
        Assert.Empty(_db.Sales);
        Assert.Empty(_db.Movements);
    }

    [Fact]
    public async Task Register_SameRequestTwice_RegistersOnlyOnce()
    {
        var dto = Sale((_cola.Id, 3));

        var first = await _service.RegisterAsync(dto);
        var second = await _service.RegisterAsync(dto);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(second.WasAlreadyRegistered);
        Assert.Equal(first.Sale!.Id, second.Sale!.Id);
        Assert.Single(_db.Sales);
        Assert.Single(_db.Movements);
        Assert.Equal(17, _cola.CurrentStock);
    }

    [Fact]
    public async Task Register_ConcurrentSalesOfLastUnits_OnlyOneSucceeds()
    {
        // Dos ventas de las 5 unidades de agua al mismo tiempo: no puede quedar stock negativo.
        var results = await Task.WhenAll(
            Task.Run(() => _service.RegisterAsync(Sale((_agua.Id, 5)))),
            Task.Run(() => _service.RegisterAsync(Sale((_agua.Id, 5)))));

        Assert.True(_agua.CurrentStock >= 0);
        Assert.Equal(1, results.Count(r => r.Succeeded));
        Assert.Single(_db.Sales);
        Assert.Equal(0, _agua.CurrentStock);
    }

    [Fact]
    public async Task Register_ManualExitsStillWork()
    {
        await _service.RegisterAsync(Sale((_cola.Id, 5)));
        var inventory = new InventoryService(_movements, new FakeProductRepository(_db));

        var exit = await inventory.RegisterExitAsync(new SIGID.Application.DTOs.RegisterMovementDto
        {
            ProductId = _cola.Id, Quantity = 5, Date = DateTime.Now.AddMinutes(-1)
        });

        Assert.Equal(10, exit.StockAfter);
        Assert.Null(exit.SaleId);
    }

    [Fact]
    public async Task RegisterSaleExits_InvalidSaleId_Throws()
    {
        var inventory = new InventoryService(_movements, new FakeProductRepository(_db));

        await Assert.ThrowsAsync<BusinessException>(() => inventory.RegisterSaleExitsAsync(0, DateTime.Now,
            new List<RegisterSaleDetailDto> { new() { ProductId = _cola.Id, Quantity = 1 } }));
        Assert.Empty(_db.Movements);
    }

    // ---------------- Comprobación al agregar o modificar (ajustes de UX) ----------------

    [Fact]
    public async Task CheckAvailability_MoreThanStock_ExplainsWithNameQuantityAndStock()
    {
        var problem = await _service.CheckAvailabilityAsync(_agua.Id, 6);

        Assert.NotNull(problem);
        Assert.True(problem!.IsStockError);
        Assert.Equal("No puedes agregar 6 UND de «Producto BEB-002» porque solamente hay 5 UND disponibles.", problem.Message);
    }

    [Fact]
    public async Task CheckAvailability_AccumulatedQuantity_IsValidated()
    {
        Assert.Null(await _service.CheckAvailabilityAsync(_agua.Id, 2));                       // 2 de 5
        Assert.Null(await _service.CheckAvailabilityAsync(_agua.Id, 3, alreadyInSale: 2));     // 2 + 3 = 5
        var problem = await _service.CheckAvailabilityAsync(_agua.Id, 4, alreadyInSale: 2);   // 2 + 4 = 6

        Assert.NotNull(problem);
        Assert.Equal("Ya tienes 2 UND de «Producto BEB-002» en la venta. No puedes agregar 4 UND más porque solamente hay 5 UND disponibles.", problem!.Message);
    }

    [Fact]
    public async Task CheckAvailability_ModifiedLine_UsesItsOwnMessage()
    {
        var problem = await _service.CheckAvailabilityAsync(_agua.Id, 9, isExistingLine: true);

        Assert.Equal("Indicaste 9 UND de «Producto BEB-002», pero solamente hay 5 UND disponibles.", problem!.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CheckAvailability_InvalidQuantity_IsRejected(int quantity)
    {
        var problem = await _service.CheckAvailabilityAsync(_cola.Id, quantity);

        Assert.NotNull(problem);
        Assert.False(problem!.IsStockError);
        Assert.Contains("mayor que cero", problem.Message);
    }

    [Fact]
    public async Task CheckAvailability_InactiveOrMissingProduct_IsRejected()
    {
        Assert.NotNull(await _service.CheckAvailabilityAsync(_inactive.Id, 1));
        Assert.NotNull(await _service.CheckAvailabilityAsync(9999, 1));
    }

    [Fact]
    public async Task CheckAvailability_DoesNotChangeStockOrSaveAnything()
    {
        await _service.CheckAvailabilityAsync(_agua.Id, 50);
        await _service.CheckAvailabilityAsync(_agua.Id, 1);

        Assert.Equal(5, _agua.CurrentStock);
        Assert.Empty(_db.Sales);
        Assert.Empty(_db.Movements);
    }

    // ---------------- Historial (HU-16) ----------------

    private Sale AddHistoricSale(DateTime date, params (Product product, int quantity)[] lines)
    {
        var sale = new Sale
        {
            Id = _db.NextId(),
            Date = date,
            RequestId = Guid.NewGuid(),
            Details = lines.Select(l => new SaleDetail { Id = _db.NextId(), ProductId = l.product.Id, Quantity = l.quantity }).ToList()
        };
        foreach (var d in sale.Details) d.SaleId = sale.Id;
        _db.Sales.Add(sale);
        return sale;
    }

    [Fact]
    public async Task History_IsOrderedFromNewestAndShowsTotals()
    {
        var older = AddHistoricSale(new DateTime(2026, 10, 1, 9, 0, 0), (_cola, 2));
        var newer = AddHistoricSale(new DateTime(2026, 10, 5, 9, 0, 0), (_cola, 1), (_agua, 3));

        var history = await _service.GetHistoryAsync(new SaleHistoryFilterDto());

        Assert.Equal(new[] { newer.Id, older.Id }, history.Select(h => h.Id).ToArray());
        Assert.Equal(2, history[0].ProductCount);
        Assert.Equal(4, history[0].TotalQuantity);
        Assert.Equal(new[] { "Producto BEB-001", "Producto BEB-002" }, history[0].Items.Select(i => i.ProductName).ToArray());
        Assert.Equal(new[] { 1, 3 }, history[0].Items.Select(i => i.Quantity).ToArray());
        Assert.All(history[0].Items, i => Assert.Equal("UND", i.UnitOfMeasure));
        Assert.Equal(1, history[1].ProductCount);
    }

    [Fact]
    public async Task History_Filters_ByProduct_Dates_AndCombined()
    {
        var s1 = AddHistoricSale(new DateTime(2026, 10, 1, 23, 30, 0), (_cola, 2));
        var s2 = AddHistoricSale(new DateTime(2026, 10, 5, 8, 0, 0), (_agua, 1));
        var s3 = AddHistoricSale(new DateTime(2026, 10, 9, 12, 0, 0), (_cola, 1), (_agua, 1));

        var byProduct = await _service.GetHistoryAsync(new SaleHistoryFilterDto { ProductId = _agua.Id });
        var fromDate = await _service.GetHistoryAsync(new SaleHistoryFilterDto { StartDate = new DateTime(2026, 10, 5) });
        var toDate = await _service.GetHistoryAsync(new SaleHistoryFilterDto { EndDate = new DateTime(2026, 10, 1) });
        var combined = await _service.GetHistoryAsync(new SaleHistoryFilterDto
        {
            ProductId = _cola.Id, StartDate = new DateTime(2026, 10, 2), EndDate = new DateTime(2026, 10, 9)
        });
        var none = await _service.GetHistoryAsync(new SaleHistoryFilterDto { StartDate = new DateTime(2026, 11, 1) });

        Assert.Equal(new[] { s3.Id, s2.Id }, byProduct.Select(h => h.Id).ToArray());
        Assert.Equal(new[] { s3.Id, s2.Id }, fromDate.Select(h => h.Id).ToArray());
        Assert.Equal(new[] { s1.Id }, toDate.Select(h => h.Id).ToArray());   // incluye todo el día final
        Assert.Equal(new[] { s3.Id }, combined.Select(h => h.Id).ToArray());
        Assert.Empty(none);
    }

    [Fact]
    public async Task History_InvertedDateRange_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.GetHistoryAsync(new SaleHistoryFilterDto
        {
            StartDate = new DateTime(2026, 10, 10), EndDate = new DateTime(2026, 10, 1)
        }));
        Assert.Contains("posterior", ex.Message);
    }

    [Fact]
    public async Task Details_ShowsProductsAndQuantities_OrNullWhenMissing()
    {
        var sale = AddHistoricSale(new DateTime(2026, 10, 5, 10, 15, 0), (_cola, 2), (_agua, 3));

        var detail = await _service.GetByIdAsync(sale.Id);
        var missing = await _service.GetByIdAsync(123456);

        Assert.NotNull(detail);
        Assert.Equal(5, detail!.TotalQuantity);
        Assert.Equal(2, detail.Details.Count);
        Assert.Contains(detail.Details, d => d.SKU == "BEB-002" && d.Quantity == 3 && d.ProductName == _agua.Name);
        Assert.Null(missing);
    }

    [Fact]
    public async Task History_Queries_DoNotModifyStockOrMovements()
    {
        await _service.RegisterAsync(Sale((_cola.Id, 2)));
        var stock = _cola.CurrentStock;
        var movements = _db.Movements.Count;

        var history = await _service.GetHistoryAsync(new SaleHistoryFilterDto());
        await _service.GetByIdAsync(history[0].Id);
        await _service.GetSoldQuantitiesAsync(DateTime.Today.AddDays(-1), DateTime.Today);

        Assert.Equal(stock, _cola.CurrentStock);
        Assert.Equal(movements, _db.Movements.Count);
        Assert.Single(_db.Sales);
    }

    [Fact]
    public async Task SoldQuantities_AreGroupedByProductAndDay()
    {
        AddHistoricSale(new DateTime(2026, 10, 1, 9, 0, 0), (_cola, 2), (_agua, 1));
        AddHistoricSale(new DateTime(2026, 10, 1, 18, 0, 0), (_cola, 3));
        AddHistoricSale(new DateTime(2026, 10, 2, 9, 0, 0), (_cola, 1));
        AddHistoricSale(new DateTime(2026, 10, 20, 9, 0, 0), (_cola, 7));   // fuera del período

        var rows = await _service.GetSoldQuantitiesAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        Assert.Equal(3, rows.Count);
        Assert.Equal(5, rows.Single(r => r.ProductId == _cola.Id && r.Day == new DateTime(2026, 10, 1)).Quantity);
        Assert.Equal(1, rows.Single(r => r.ProductId == _agua.Id).Quantity);
        Assert.Equal(1, rows.Single(r => r.ProductId == _cola.Id && r.Day == new DateTime(2026, 10, 2)).Quantity);
    }
}
