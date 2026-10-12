using System.ComponentModel.DataAnnotations;
using SIGID.Application.DTOs;

namespace SIGID.Web.ViewModels;

public class SupplierViewModel
{
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Identificación / RNC")]
    [StringLength(30, ErrorMessage = "La identificación no puede superar los {1} caracteres.")]
    public string? Identification { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(30, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
    [RegularExpression(@"^[0-9+()\-\s]{7,30}$", ErrorMessage = "El teléfono solo admite números, espacios, +, - y paréntesis (mínimo 7 caracteres).")]
    public string? Phone { get; set; }

    [Display(Name = "Correo electrónico")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los {1} caracteres.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string? Email { get; set; }

    [Display(Name = "Dirección")]
    [StringLength(250, ErrorMessage = "La dirección no puede superar los {1} caracteres.")]
    public string? Address { get; set; }

    [Display(Name = "Proveedor activo")]
    public bool IsActive { get; set; } = true;

    public int ProductCount { get; set; }

    public SupplierDto ToDto() => new()
    {
        Id = Id,
        Name = Name,
        Identification = Identification,
        Phone = Phone,
        Email = Email,
        Address = Address,
        IsActive = IsActive
    };

    public static SupplierViewModel FromDto(SupplierDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Identification = dto.Identification,
        Phone = dto.Phone,
        Email = dto.Email,
        Address = dto.Address,
        IsActive = dto.IsActive,
        ProductCount = dto.ProductCount
    };
}
