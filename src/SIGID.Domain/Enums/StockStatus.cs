namespace SIGID.Domain.Enums;

/// <summary>
/// Situación de las existencias de un producto respecto a su stock mínimo.
/// </summary>
public enum StockStatus
{
    OutOfStock = 0,
    Low = 1,
    Normal = 2
}
