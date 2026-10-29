using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace SIGID.Web.Security;

/// <summary>
/// Guarda la sesión del administrador en el servidor; la cookie solo lleva una clave aleatoria.
/// Al cerrar sesión la entrada se elimina, por lo que una copia anterior de la cookie deja de servir.
/// </summary>
/// <remarks>
/// Las sesiones viven en memoria: si la aplicación se reinicia, el administrador debe iniciar sesión de nuevo.
/// </remarks>
public class MemorySessionTicketStore : ITicketStore
{
    private const string KeyPrefix = "sigid-auth-";
    private readonly IMemoryCache _cache;

    public MemorySessionTicketStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid().ToString("N");
        Save(key, ticket);
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        // Se llama cuando la expiración deslizante extiende la sesión.
        Save(key, ticket);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _cache.TryGetValue(key, out AuthenticationTicket? ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    private void Save(string key, AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions();
        if (ticket.Properties.ExpiresUtc is DateTimeOffset expires)
            options.SetAbsoluteExpiration(expires);

        _cache.Set(key, ticket, options);
    }
}

/// <summary>
/// Conecta el almacén de sesiones con el esquema de cookies.
/// </summary>
public class ConfigureTicketStore : Microsoft.Extensions.Options.IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly ITicketStore _store;

    public ConfigureTicketStore(ITicketStore store)
    {
        _store = store;
    }

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        if (name == CookieAuthenticationDefaults.AuthenticationScheme)
            options.SessionStore = _store;
    }
}
