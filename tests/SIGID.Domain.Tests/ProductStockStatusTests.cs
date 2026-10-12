using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Domain.Tests;

public class ProductStockStatusTests
{
    [Fact]
    public void ZeroStock_IsOutOfStock()
    {
        var product = new Product { CurrentStock = 0, MinimumStock = 5 };
        Assert.Equal(StockStatus.OutOfStock, product.GetStockStatus());
    }

    [Fact]
    public void StockAtMinimum_IsLow()
    {
        Assert.Equal(StockStatus.Low, Product.GetStockStatus(5, 5));
        Assert.Equal(StockStatus.Low, Product.GetStockStatus(1, 5));
    }

    [Fact]
    public void StockAboveMinimum_IsNormal()
    {
        Assert.Equal(StockStatus.Normal, Product.GetStockStatus(6, 5));
    }

    [Fact]
    public void NewProduct_StartsWithoutStock()
    {
        var product = new Product();
        Assert.Equal(0, product.CurrentStock);
        Assert.True(product.IsActive);
    }
}
