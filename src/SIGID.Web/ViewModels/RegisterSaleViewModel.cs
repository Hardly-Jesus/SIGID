using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Sales;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Formulario de registro de una venta (HU-14).
/// </summary>
public class RegisterSaleViewModel
{
    /// <summary>
    /// Identificador del formulario. Si se envía dos veces, la venta se registra una sola vez.
    /// </summary>
    public Guid RequestId { get; set; }

    // Producto y cantidad que se van a agregar a la venta.
    [Display(Name = "Producto")]
    public int? NewProductId { get; set; }

    [Display(Name = "Cantidad")]
    public int? NewQuantity { get; set; }

    public List<RegisterSaleDetailViewModel> Lines { get; set; } = new();

    /// <summary>
    /// Productos activos que se pueden vender, cargados desde la base de datos.
    /// </summary>
    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public int TotalQuantity => Lines.Sum(l => l.Quantity ?? 0);

    /// <summary>
    /// Aviso de error que se muestra junto al formulario (no se recibe del navegador).
    /// </summary>
    [BindNever]
    public string? ErrorTitle { get; set; }

    [BindNever]
    public List<string> ErrorMessages { get; } = new();

    public RegisterSaleDto ToDto() => new()
    {
        RequestId = RequestId,
        Details = Lines
            .Select(l => new RegisterSaleDetailDto { ProductId = l.ProductId, Quantity = l.Quantity ?? 0 })
            .ToList()
    };
}
