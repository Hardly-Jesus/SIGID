namespace SIGID.Application.DTOs;

public class SupplierDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Identification { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cantidad de productos asociados (solo lectura).
    /// </summary>
    public int ProductCount { get; set; }
}
