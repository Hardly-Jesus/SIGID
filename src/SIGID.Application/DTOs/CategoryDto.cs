namespace SIGID.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cantidad de productos asociados (solo lectura).
    /// </summary>
    public int ProductCount { get; set; }
}
