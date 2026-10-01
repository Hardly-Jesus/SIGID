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
    /// Identificación tributaria o documento del proveedor (NIT, RUC, etc.).
    /// </summary>
    public string Identification { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
