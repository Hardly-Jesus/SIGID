using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;
using SIGID.Infrastructure.Persistence;
using SIGID.Infrastructure.Persistence.Repositories;

namespace SIGID.Infrastructure.Tests;

/// <summary>
/// Pruebas de las consultas de EF Core con una base SQLite en memoria.
/// Comprueban la lógica de las consultas; no reemplazan una prueba contra SQL Server.
/// </summary>
public class DemandRepositoriesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SigidDbContext _context;
    private readonly Product _product;
    private readonly Product _otherProduct;

    public DemandRepositoriesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<SigidDbContext>().UseSqlite(_connection).Options;
        _context = new SigidDbContext(options);
        _context.Database.EnsureCreated();   // base temporal en memoria, solo para esta prueba

        var category = new Category { Name = "Bebidas", IsActive = true };
        var supplier = new Supplier { Name = "Proveedor", IsActive = true };
        _product = NewProduct("A", category, supplier);
        _otherProduct = NewProduct("B", category, supplier);
        _context.AddRange(_product, _otherProduct);
        _context.SaveChanges();
    }

    private static Product NewProduct(string sku, Category category, Supplier supplier) => new()
    {
        SKU = sku, Name = "Producto " + sku, Category = category, Supplier = supplier,
        UnitOfMeasure = "UND", MinimumStock = 1, MaximumStock = 100, CurrentStock = 20, IsActive = true
    };

    private void AddSale(DateTime date, params (Product Product, int Quantity)[] lines)
    {
        var sale = new Sale { Date = date, RequestId = Guid.NewGuid() };
        foreach (var (product, quantity) in lines)
            sale.Details.Add(new SaleDetail { ProductId = product.Id, Quantity = quantity });
        _context.Sales.Add(sale);
        _context.SaveChanges();
    }

    [Fact]
    public async Task SaleRepository_DailyQuantities_GroupsByDayAndProduct()
    {
        AddSale(new DateTime(2026, 10, 1, 9, 0, 0), (_product, 2), (_otherProduct, 7));
        AddSale(new DateTime(2026, 10, 1, 20, 15, 0), (_product, 3));
        AddSale(new DateTime(2026, 10, 3, 23, 59, 0), (_product, 1));   // último minuto del día
        AddSale(new DateTime(2026, 10, 4, 0, 0, 0), (_product, 9));     // fuera del rango

        var repository = new SaleRepository(_context);
        var result = await repository.GetDailyQuantitiesAsync(_product.Id, new DateTime(2026, 10, 1), new DateTime(2026, 10, 3));

        Assert.Equal(2, result.Count);
        Assert.Equal(5, result[new DateTime(2026, 10, 1)]);
        Assert.Equal(1, result[new DateTime(2026, 10, 3)]);
        Assert.Equal(new DateTime(2026, 10, 1, 9, 0, 0), await repository.GetFirstSaleDateAsync());
    }

    [Fact]
    public async Task SyntheticRepository_SavesBatch_QueriesAndDeletesInCascade()
    {
        var repository = new SyntheticSalesRepository(_context);
        var batch = new SyntheticSalesBatch
        {
            CreatedAt = new DateTime(2026, 11, 6), StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31),
            Pattern = DemandPattern.Seasonal, RandomSeed = 42, ProductIds = $"{_product.Id}", ProductCount = 1,
            ParametersKey = "clave-1", RecordCount = 2
        };
        batch.Records.Add(new SyntheticSalesRecord { ProductId = _product.Id, Date = new DateTime(2026, 1, 5), Quantity = 4 });
        batch.Records.Add(new SyntheticSalesRecord { ProductId = _product.Id, Date = new DateTime(2026, 1, 6), Quantity = 2 });

        await repository.AddBatchAsync(batch);

        Assert.NotNull(await repository.GetBatchByKeyAsync("clave-1"));
        Assert.Equal(new[] { _product.Id }, await repository.GetProductsWithDataAsync(new[] { _product.Id, _otherProduct.Id }, new DateTime(2026, 1, 20), new DateTime(2026, 2, 10)));
        Assert.Empty(await repository.GetProductsWithDataAsync(new[] { _product.Id }, new DateTime(2026, 2, 1), new DateTime(2026, 2, 10)));
        Assert.Equal(4, (await repository.GetDailyQuantitiesAsync(_product.Id, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)))[new DateTime(2026, 1, 5)]);
        Assert.Equal((new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)), Assert.Single(await repository.GetCoverageAsync(_product.Id)));
        Assert.Empty(await repository.GetCoverageAsync(_otherProduct.Id));

        // Los datos de prueba no tocan existencias ni ventas reales.
        Assert.Equal(20, (await _context.Products.AsNoTracking().FirstAsync(p => p.Id == _product.Id)).CurrentStock);
        Assert.Equal(0, await _context.Sales.CountAsync());
        Assert.Equal(0, await _context.InventoryMovements.CountAsync());

        Assert.True(await repository.DeleteBatchAsync(batch.Id));
        Assert.Equal(0, await _context.SyntheticSalesRecords.CountAsync());
    }

    [Fact]
    public async Task SyntheticRepository_DuplicateKey_IsRejectedAndNothingIsSaved()
    {
        var repository = new SyntheticSalesRepository(_context);
        await repository.AddBatchAsync(NewBatch("misma-clave"));

        using var otherContext = new SigidDbContext(new DbContextOptionsBuilder<SigidDbContext>().UseSqlite(_connection).Options);
        var otherRepository = new SyntheticSalesRepository(otherContext);

        await Assert.ThrowsAsync<SIGID.Application.Exceptions.BusinessException>(() => otherRepository.AddBatchAsync(NewBatch("misma-clave")));
        Assert.Equal(1, await _context.SyntheticSalesBatches.CountAsync());
        Assert.Equal(1, await _context.SyntheticSalesRecords.CountAsync());
    }

    private SyntheticSalesBatch NewBatch(string key)
    {
        var batch = new SyntheticSalesBatch
        {
            CreatedAt = new DateTime(2026, 11, 6), StartDate = new DateTime(2026, 3, 1), EndDate = new DateTime(2026, 3, 2),
            Pattern = DemandPattern.Stable, RandomSeed = 1, ProductIds = $"{_product.Id}", ProductCount = 1, ParametersKey = key, RecordCount = 1
        };
        batch.Records.Add(new SyntheticSalesRecord { ProductId = _product.Id, Date = new DateTime(2026, 3, 1), Quantity = 1 });
        return batch;
    }

    [Fact]
    public async Task SaleRepository_SearchPage_ReturnsOnlyRequestedPage()
    {
        for (var i = 0; i < 60; i++)
            AddSale(new DateTime(2026, 1, 1).AddDays(i), (_product, 1), (_otherProduct, 2));

        var repository = new SaleRepository(_context);
        var (page2, total) = await repository.SearchPageAsync(null, null, null, 2, 25);
        var (filtered, filteredTotal) = await repository.SearchPageAsync(_product.Id, new DateTime(2026, 1, 11), new DateTime(2026, 1, 20, 23, 59, 59), 1, 50);

        Assert.Equal(60, total);
        Assert.Equal(25, page2.Count);
        Assert.Equal(new DateTime(2026, 1, 1).AddDays(34), page2[0].Date);   // más recientes primero
        Assert.All(page2, s => Assert.Equal(2, s.Details.Count));          // los detalles se cargan
        Assert.Equal(10, filteredTotal);
        Assert.Equal(10, filtered.Count);
    }

    [Fact]
    public async Task MovementRepository_SearchPageAndTotals()
    {
        for (var i = 0; i < 30; i++)
        {
            _context.InventoryMovements.Add(new InventoryMovement { ProductId = _product.Id, MovementType = MovementType.Entry, Quantity = 4, StockAfter = 0, Date = new DateTime(2026, 2, 1).AddHours(i) });
            _context.InventoryMovements.Add(new InventoryMovement { ProductId = _product.Id, MovementType = MovementType.Exit, Quantity = 1, StockAfter = 0, Date = new DateTime(2026, 2, 1).AddHours(i).AddMinutes(30) });
        }
        _context.SaveChanges();

        var repository = new InventoryMovementRepository(_context);
        var (items, total) = await repository.SearchPageAsync(_product.Id, null, null, null, 3, 25);
        var (entries, exits) = await repository.GetQuantityTotalsAsync(_product.Id, null, null, null);

        Assert.Equal(60, total);
        Assert.Equal(10, items.Count);
        Assert.All(items, m => Assert.NotNull(m.Product));
        Assert.Equal(120, entries);
        Assert.Equal(30, exits);
    }

    [Fact]
    public async Task SaleRepository_FirstSaleDateOfProduct()
    {
        AddSale(new DateTime(2026, 3, 5, 9, 0, 0), (_otherProduct, 1));
        AddSale(new DateTime(2026, 4, 2, 9, 0, 0), (_product, 1));
        AddSale(new DateTime(2026, 5, 1, 9, 0, 0), (_product, 1));

        var repository = new SaleRepository(_context);

        Assert.Equal(new DateTime(2026, 4, 2, 9, 0, 0), await repository.GetFirstSaleDateAsync(_product.Id));
        Assert.Null(await repository.GetFirstSaleDateAsync(999));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
