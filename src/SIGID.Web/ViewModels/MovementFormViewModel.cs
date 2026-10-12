using System.ComponentModel.DataAnnotations;
using SIGID.Application.DTOs;
using SIGID.Domain.Enums;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Formulario de registro de una entrada o salida de inventario.
/// </summary>
public class MovementFormViewModel
{
    /// <summary>
    /// Lo define el controlador según la acción (CreateEntry / CreateExit), no el usuario.
    /// </summary>
    public MovementType MovementType { get; set; }

    [Display(Name = "Producto")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un producto.")]
    public int ProductId { get; set; }

    [Display(Name = "Cantidad")]
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe ser mayor que cero (máximo 1.000.000).")]
    public int? Quantity { get; set; }

    [Display(Name = "Fecha y hora")]
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateTime? Date { get; set; }

    [Display(Name = "Observación")]
    [StringLength(250, ErrorMessage = "La observación no puede superar los {1} caracteres.")]
    public string? Description { get; set; }

    /// <summary>
    /// Productos activos disponibles, cargados desde la base de datos.
    /// </summary>
    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public bool IsEntry => MovementType == MovementType.Entry;

    public ProductDto? SelectedProduct => Products.FirstOrDefault(p => p.Id == ProductId);

    public RegisterMovementDto ToDto() => new()
    {
        ProductId = ProductId,
        Quantity = Quantity ?? 0,
        Date = Date ?? default,
        Description = Description
    };
}
