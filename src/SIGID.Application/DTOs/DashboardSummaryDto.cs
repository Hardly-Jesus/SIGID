namespace SIGID.Application.DTOs;

/// <summary>
/// Indicadores del dashboard calculados a partir de los datos reales.
/// </summary>
public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int TotalCategories { get; set; }

    public int TotalSuppliers { get; set; }

    public int OutOfStockProducts { get; set; }

    public int LowStockProducts { get; set; }

    public int TotalUnitsInStock { get; set; }

    /// <summary>
    /// Productos activos que requieren atención (sin existencias o con stock bajo).
    /// </summary>
    public IReadOnlyList<ProductDto> ProductsNeedingAttention { get; set; } = Array.Empty<ProductDto>();

    public IReadOnlyList<InventoryMovementDto> RecentMovements { get; set; } = Array.Empty<InventoryMovementDto>();
}
