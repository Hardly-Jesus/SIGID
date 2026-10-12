using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SIGID.Application.DTOs;
using SIGID.Domain.Enums;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Modelo de la vista de productos con las validaciones del formulario.
/// Las mismas reglas se vuelven a validar en ProductService.
/// </summary>
public class ProductViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "SKU")]
    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(50, ErrorMessage = "El SKU no puede superar los {1} caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "El SKU solo admite letras, números, guiones y guiones bajos.")]
    public string SKU { get; set; } = string.Empty;

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    public string? Description { get; set; }

    [Display(Name = "Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Display(Name = "Proveedor")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un proveedor.")]
    public int SupplierId { get; set; }

    public string? SupplierName { get; set; }

    [Display(Name = "Unidad de medida")]
    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(20, ErrorMessage = "La unidad de medida no puede superar los {1} caracteres.")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Display(Name = "Stock mínimo")]
    [Range(0, 1_000_000, ErrorMessage = "El stock mínimo debe estar entre 0 y 1.000.000.")]
    public int MinimumStock { get; set; }

    [Display(Name = "Stock máximo")]
    [Range(0, 1_000_000, ErrorMessage = "El stock máximo debe estar entre 0 y 1.000.000.")]
    public int MaximumStock { get; set; }

    /// <summary>
    /// Solo lectura: las existencias cambian con entradas y salidas de inventario.
    /// </summary>
    public int CurrentStock { get; set; }

    public StockStatus StockStatus { get; set; }

    [Display(Name = "Producto activo")]
    public bool IsActive { get; set; } = true;

    // Opciones de los desplegables, cargadas desde la base de datos por el controlador.
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Suppliers { get; set; } = Enumerable.Empty<SelectListItem>();

    /// <summary>
    /// Unidades de medida permitidas (opciones fijas del dominio).
    /// </summary>
    public static IEnumerable<SelectListItem> UnitsOfMeasure { get; } = new[]
    {
        new SelectListItem("Unidad (UND)", "UND"),
        new SelectListItem("Kilogramo (KG)", "KG"),
        new SelectListItem("Gramo (G)", "G"),
        new SelectListItem("Litro (LT)", "LT"),
        new SelectListItem("Mililitro (ML)", "ML"),
        new SelectListItem("Metro (M)", "M"),
        new SelectListItem("Caja (CAJA)", "CAJA"),
        new SelectListItem("Paquete (PAQ)", "PAQ"),
        new SelectListItem("Docena (DOC)", "DOC")
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaximumStock < MinimumStock)
        {
            yield return new ValidationResult(
                "El stock máximo debe ser mayor o igual al stock mínimo.",
                new[] { nameof(MaximumStock) });
        }
    }

    public ProductDto ToDto() => new()
    {
        Id = Id,
        SKU = SKU,
        Name = Name,
        Description = Description,
        CategoryId = CategoryId,
        SupplierId = SupplierId,
        UnitOfMeasure = UnitOfMeasure,
        MinimumStock = MinimumStock,
        MaximumStock = MaximumStock,
        IsActive = IsActive
    };

    public static ProductViewModel FromDto(ProductDto dto) => new()
    {
        Id = dto.Id,
        SKU = dto.SKU,
        Name = dto.Name,
        Description = dto.Description,
        CategoryId = dto.CategoryId,
        CategoryName = dto.CategoryName,
        SupplierId = dto.SupplierId,
        SupplierName = dto.SupplierName,
        UnitOfMeasure = dto.UnitOfMeasure,
        MinimumStock = dto.MinimumStock,
        MaximumStock = dto.MaximumStock,
        CurrentStock = dto.CurrentStock,
        StockStatus = dto.StockStatus,
        IsActive = dto.IsActive
    };
}
