namespace SIGID.Application.Interfaces.Persistence;

/// <summary>
/// Ejecuta varias operaciones de base de datos como una sola transacción.
/// Si la operación termina bien se confirma; si lanza una excepción se revierte todo.
/// La implementación con Entity Framework está en Infrastructure.
/// </summary>
public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
