namespace SIGID.Application.DTOs.Authentication;

public enum InitialAdministratorResult
{
    /// <summary>Se creó la cuenta inicial.</summary>
    Created,

    /// <summary>Ya existe al menos un administrador; no se hizo nada.</summary>
    AlreadyExists,

    /// <summary>No hay administrador y la configuración está incompleta.</summary>
    NotConfigured
}
