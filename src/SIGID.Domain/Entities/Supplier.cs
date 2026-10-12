namespace SIGID.Domain.Entities;

/// <summary>
/// Proveedor de productos.
/// Relación: Supplier 1 ──── N Product
/// </summary>
public class Supplier
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identificación tributaria del proveedor (RNC, NIT, etc.).
    /// Es opcional, pero si se indica debe ser única.
    /// </summary>
    public string? Identification { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
