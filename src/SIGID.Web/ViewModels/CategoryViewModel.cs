using System.ComponentModel.DataAnnotations;
using SIGID.Application.DTOs;

namespace SIGID.Web.ViewModels;

public class CategoryViewModel
{
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    public string? Description { get; set; }

    [Display(Name = "Categoría activa")]
    public bool IsActive { get; set; } = true;

    public int ProductCount { get; set; }

    public CategoryDto ToDto() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        IsActive = IsActive
    };

    public static CategoryViewModel FromDto(CategoryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        IsActive = dto.IsActive,
        ProductCount = dto.ProductCount
    };
}
