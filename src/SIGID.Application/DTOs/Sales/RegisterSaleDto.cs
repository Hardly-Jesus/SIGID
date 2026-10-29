namespace SIGID.Application.DTOs.Sales;

/// <summary>
/// Datos para registrar una venta.
/// </summary>
public class RegisterSaleDto
{
    /// <summary>
    /// Identificador del formulario. Si llega dos veces el mismo, la venta no se repite.
    /// </summary>
    public Guid RequestId { get; set; }

    public List<RegisterSaleDetailDto> Details { get; set; } = new();
}

public class RegisterSaleDetailDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}
