namespace SIGID.Application.DTOs;

/// <summary>
/// Datos para registrar una entrada o una salida de inventario.
/// El tipo de movimiento lo define el método del servicio que se use.
/// </summary>
public class RegisterMovementDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime Date { get; set; }

    public string? Description { get; set; }
}
