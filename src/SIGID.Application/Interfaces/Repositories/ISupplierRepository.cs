using SIGID.Domain.Entities;

namespace SIGID.Application.Interfaces.Repositories;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un proveedor por nombre sin distinguir mayúsculas/minúsculas.
    /// </summary>
    Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<Supplier?> GetByIdentificationAsync(string identification, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<bool> HasProductsAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidad de productos por proveedor (clave: Id del proveedor).
    /// </summary>
    Task<Dictionary<int, int>> GetProductCountsAsync(CancellationToken cancellationToken = default);
}
