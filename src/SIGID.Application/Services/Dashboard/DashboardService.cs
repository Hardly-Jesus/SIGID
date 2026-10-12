using SIGID.Application.DTOs;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Application.Services.Inventory;
using SIGID.Application.Services.Products;
using SIGID.Domain.Enums;

namespace SIGID.Application.Services.Dashboard;

/// <summary>
/// Calcula los indicadores del dashboard a partir de los datos almacenados.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int RecentMovementsCount = 6;

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IInventoryMovementRepository _movementRepository;

    public DashboardService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository,
        IInventoryMovementRepository movementRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _movementRepository = movementRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
        var recent = await _movementRepository.GetRecentAsync(RecentMovementsCount, cancellationToken);

        var active = products.Where(p => p.IsActive).ToList();

        return new DashboardSummaryDto
        {
            TotalProducts = products.Count,
            ActiveProducts = active.Count,
            TotalCategories = categories.Count,
            TotalSuppliers = suppliers.Count,
            OutOfStockProducts = active.Count(p => p.GetStockStatus() == StockStatus.OutOfStock),
            LowStockProducts = active.Count(p => p.GetStockStatus() == StockStatus.Low),
            TotalUnitsInStock = active.Sum(p => p.CurrentStock),
            ProductsNeedingAttention = active
                .Where(p => p.GetStockStatus() != StockStatus.Normal)
                .OrderBy(p => p.CurrentStock)
                .ThenBy(p => p.Name)
                .Take(6)
                .Select(ProductService.MapToDto)
                .ToList(),
            RecentMovements = recent.Select(InventoryService.MapToDto).ToList()
        };
    }
}
