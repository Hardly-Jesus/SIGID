namespace SIGID.Domain.Entities;

/// <summary>
/// Categoría a la que pertenecen los productos.
/// Relación: Category 1 ──── N Product
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
