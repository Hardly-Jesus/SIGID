using SIGID.Application.DTOs;

namespace SIGID.Application.Interfaces.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(SupplierDto supplier, CancellationToken cancellationToken = default);

    Task UpdateAsync(SupplierDto supplier, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
