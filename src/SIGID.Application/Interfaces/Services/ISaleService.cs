using SIGID.Application.DTOs.Sales;

namespace SIGID.Application.Interfaces.Services;

public interface ISaleService
{
    /// <summary>
    /// Registra la venta, descuenta las existencias y genera los movimientos de salida
    /// en una sola transacción. Si algo falla, no se guarda nada.
    /// </summary>
    Task<SaleRegistrationResultDto> RegisterAsync(RegisterSaleDto sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Comprueba, antes de confirmar, si se puede vender esa cantidad de un producto.
    /// alreadyInSale: unidades de ese producto que ya están en la venta (al agregar más).
    /// isExistingLine: true cuando se valida la cantidad modificada de una línea ya agregada.
    /// Devuelve null si todo está bien. La validación definitiva se repite al registrar la venta.
    /// </summary>
    Task<SaleErrorDto?> CheckAvailabilityAsync(int productId, int quantity, int alreadyInSale = 0, bool isExistingLine = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial con filtros combinables, de la venta más reciente a la más antigua.
    /// </summary>
    Task<IReadOnlyList<SaleHistoryDto>> GetHistoryAsync(SaleHistoryFilterDto filter, CancellationToken cancellationToken = default);

    Task<SaleHistoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidades vendidas por producto y día en un período (información para EP-05).
    /// </summary>
    Task<IReadOnlyList<ProductSalesByDayDto>> GetSoldQuantitiesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
