using System.ComponentModel.DataAnnotations;
using SIGID.Application.DTOs;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Una línea de la venta: producto y cantidad.
/// </summary>
public class RegisterSaleDetailViewModel
{
    public int ProductId { get; set; }

    [Display(Name = "Cantidad")]
    [Required(ErrorMessage = "Indique la cantidad.")]
    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int? Quantity { get; set; }

    /// <summary>
    /// Datos del producto para mostrar en la tabla. Los carga el controlador desde la base de datos,
    /// no se reciben del formulario.
    /// </summary>
    public ProductDto? Product { get; set; }

    // Solo informativo: la validación real de existencias la hace SaleService.
    public bool ExceedsStock => Product is not null && Quantity > Product.CurrentStock;
}
