using SIGID.Domain.Entities;

namespace SIGID.Application.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca una categoría por nombre sin distinguir mayúsculas/minúsculas.
    /// </summary>
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task DeleteAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidad de productos por categoría (clave: Id de la categoría).
    /// </summary>
    Task<Dictionary<int, int>> GetProductCountsAsync(CancellationToken cancellationToken = default);
}
