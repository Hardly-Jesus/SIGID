using SIGID.Application.Services.Dashboard;
using SIGID.Application.Tests.Fakes;

namespace SIGID.Application.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task Summary_IsCalculatedFromStoredData()
    {
        var db = new InMemoryDatabase();
        var category = db.AddCategory("Bebidas");
        var supplier = db.AddSupplier("Proveedor");
        db.AddProduct("A", category, supplier, currentStock: 0, minimumStock: 5);   // sin existencias
        db.AddProduct("B", category, supplier, currentStock: 3, minimumStock: 5);   // stock bajo
        db.AddProduct("C", category, supplier, currentStock: 50, minimumStock: 5);  // normal
        db.AddProduct("D", category, supplier, currentStock: 0, isActive: false);   // inactivo: no cuenta

        var service = new DashboardService(
            new FakeProductRepository(db),
            new FakeCategoryRepository(db),
            new FakeSupplierRepository(db),
            new FakeInventoryMovementRepository(db));

        var summary = await service.GetSummaryAsync();

        Assert.Equal(4, summary.TotalProducts);
        Assert.Equal(3, summary.ActiveProducts);
        Assert.Equal(1, summary.TotalCategories);
        Assert.Equal(1, summary.TotalSuppliers);
        Assert.Equal(1, summary.OutOfStockProducts);
        Assert.Equal(1, summary.LowStockProducts);
        Assert.Equal(53, summary.TotalUnitsInStock);
        Assert.Equal(2, summary.ProductsNeedingAttention.Count);
        Assert.Empty(summary.RecentMovements);
    }
}
