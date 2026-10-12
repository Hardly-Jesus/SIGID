using Microsoft.EntityFrameworkCore;
using SIGID.Application.Exceptions;

namespace SIGID.Infrastructure.Persistence;

/// <summary>
/// Guarda los cambios y convierte los errores de base de datos en mensajes comprensibles.
/// Las reglas se validan antes en Application; esto cubre casos poco comunes,
/// como dos usuarios guardando el mismo dato al mismo tiempo.
/// </summary>
internal static class PersistenceErrors
{
    public static async Task SaveChangesAsync(DbContext context, string friendlyMessage, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Se descartan los cambios pendientes para no dejar el contexto en un estado inválido.
            context.ChangeTracker.Clear();
            throw new BusinessException(friendlyMessage);
        }
    }
}
