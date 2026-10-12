using SIGID.Application.DTOs;

namespace SIGID.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CategoryDto category, CancellationToken cancellationToken = default);

    Task UpdateAsync(CategoryDto category, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
