namespace SIGID.Domain.Entities;

/// <summary>
/// Venta registrada en el sistema. Cada venta tiene uno o varios productos (detalles).
/// </summary>
public class Sale
{
    public int Id { get; set; }

    /// <summary>
    /// Fecha y hora en que se registró la venta.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Identificador único de la solicitud que creó la venta.
    /// Sirve para no registrar dos veces la misma venta si el formulario se envía repetido.
    /// </summary>
    public Guid RequestId { get; set; }

    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}
