namespace SIGID.Application.DTOs.Sales;

/// <summary>
/// Resultado del registro de una venta: la venta guardada o los errores que la impidieron.
/// </summary>
public class SaleRegistrationResultDto
{
    public bool Succeeded { get; init; }

    /// <summary>
    /// true cuando el mismo formulario ya se había registrado antes (envío repetido).
    /// </summary>
    public bool WasAlreadyRegistered { get; init; }

    public SaleDto? Sale { get; init; }

    public IReadOnlyList<SaleErrorDto> Errors { get; init; } = Array.Empty<SaleErrorDto>();

    public static SaleRegistrationResultDto Success(SaleDto sale, bool wasAlreadyRegistered = false) => new()
    {
        Succeeded = true,
        Sale = sale,
        WasAlreadyRegistered = wasAlreadyRegistered
    };

    public static SaleRegistrationResultDto Failure(IReadOnlyList<SaleErrorDto> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    public static SaleRegistrationResultDto Failure(string message) =>
        Failure(new[] { new SaleErrorDto { Message = message } });
}

/// <summary>
/// Error de validación de una venta. ProductId indica el producto afectado (si aplica).
/// </summary>
public class SaleErrorDto
{
    public int? ProductId { get; init; }

    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// true cuando el problema es la falta de existencias (la pantalla muestra un título específico).
    /// </summary>
    public bool IsStockError { get; init; }
}
