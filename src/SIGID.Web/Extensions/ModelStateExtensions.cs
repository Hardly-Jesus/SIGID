using Microsoft.AspNetCore.Mvc.ModelBinding;
using SIGID.Application.Exceptions;

namespace SIGID.Web.Extensions;

public static class ModelStateExtensions
{
    /// <summary>
    /// Muestra el error de negocio junto al campo correspondiente (o arriba del formulario si no hay campo).
    /// </summary>
    public static void AddBusinessError(this ModelStateDictionary modelState, BusinessException exception)
    {
        modelState.AddModelError(exception.PropertyName ?? string.Empty, exception.Message);
    }
}
