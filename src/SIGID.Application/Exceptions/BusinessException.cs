namespace SIGID.Application.Exceptions;

/// <summary>
/// Error de regla de negocio. El mensaje se muestra tal cual al usuario.
/// PropertyName indica el campo del formulario relacionado (si aplica).
/// </summary>
public class BusinessException : Exception
{
    public string? PropertyName { get; }

    public BusinessException(string message, string? propertyName = null)
        : base(message)
    {
        PropertyName = propertyName;
    }
}
