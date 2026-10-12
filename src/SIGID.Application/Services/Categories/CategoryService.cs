using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;

namespace SIGID.Application.Services.Categories;

/// <summary>
/// Lógica de negocio de categorías (HU-06).
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var counts = await _categoryRepository.GetProductCountsAsync(cancellationToken);

        return categories
            .Select(c => MapToDto(c, counts.GetValueOrDefault(c.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(c => c.IsActive).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return null;

        var counts = await _categoryRepository.GetProductCountsAsync(cancellationToken);
        return MapToDto(category, counts.GetValueOrDefault(category.Id));
    }

    public async Task<int> CreateAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(dto, excludeId: null, cancellationToken);

        var category = new Category();
        MapToEntity(dto, category);

        await _categoryRepository.AddAsync(category, cancellationToken);
        return category.Id;
    }

    public async Task UpdateAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new BusinessException("La categoría que intenta editar no existe.");

        await ValidateAsync(dto, excludeId: dto.Id, cancellationToken);

        MapToEntity(dto, category);
        await _categoryRepository.UpdateAsync(category, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessException("La categoría que intenta eliminar no existe.");

        // Regla: no se puede eliminar una categoría que tenga productos asociados.
        if (await _categoryRepository.HasProductsAsync(id, cancellationToken))
            throw new BusinessException(
                $"No se puede eliminar la categoría '{category.Name}' porque tiene productos asociados. " +
                "Puede marcarla como inactiva.");

        await _categoryRepository.DeleteAsync(category, cancellationToken);
    }

    private async Task ValidateAsync(CategoryDto dto, int? excludeId, CancellationToken cancellationToken)
    {
        var name = dto.Name?.Trim() ?? string.Empty;

        if (name.Length == 0)
            throw new BusinessException("El nombre de la categoría es obligatorio.", nameof(CategoryDto.Name));

        if (name.Length > 100)
            throw new BusinessException("El nombre no puede superar los 100 caracteres.", nameof(CategoryDto.Name));

        if (dto.Description?.Trim().Length > 500)
            throw new BusinessException("La descripción no puede superar los 500 caracteres.", nameof(CategoryDto.Description));

        var existing = await _categoryRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null && existing.Id != excludeId)
            throw new BusinessException($"Ya existe una categoría con el nombre '{existing.Name}'.", nameof(CategoryDto.Name));
    }

    private static CategoryDto MapToDto(Category category, int productCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        ProductCount = productCount
    };

    private static void MapToEntity(CategoryDto dto, Category category)
    {
        category.Name = dto.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        category.IsActive = dto.IsActive;
    }
}
