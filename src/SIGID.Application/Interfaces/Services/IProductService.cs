using SIGID.Application.DTOs;

namespace SIGID.Application.Interfaces.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(ProductDto product, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductDto product, CancellationToken cancellationToken = default);
}
