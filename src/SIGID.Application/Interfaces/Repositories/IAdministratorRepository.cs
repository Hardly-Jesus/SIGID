using SIGID.Domain.Entities;

namespace SIGID.Application.Interfaces.Repositories;

public interface IAdministratorRepository
{
    Task<Administrator?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Administrator?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<Administrator?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca por nombre de usuario o por correo (sin distinguir mayúsculas/minúsculas).
    /// </summary>
    Task<Administrator?> GetByUserNameOrEmailAsync(string userNameOrEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si existe al menos un administrador registrado.
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Administrator administrator, CancellationToken cancellationToken = default);
}
