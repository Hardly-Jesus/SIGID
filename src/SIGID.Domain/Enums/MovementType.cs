namespace SIGID.Domain.Enums;

/// <summary>
/// Tipo de movimiento de inventario.
/// Se guarda en la base de datos como texto ("Entry" / "Exit").
/// </summary>
public enum MovementType
{
    Entry = 1,
    Exit = 2
}
