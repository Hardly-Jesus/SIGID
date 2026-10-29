namespace SIGID.Web.Extensions;

/// <summary>
/// Formatea cantidades de varios productos sin sumar unidades de medida distintas.
/// </summary>
public static class QuantityFormat
{
    /// <summary>
    /// Total por unidad de medida, por ejemplo "9 UND" o "5 UND · 2 CAJA".
    /// </summary>
    public static string TotalsByUnit(IEnumerable<(int Quantity, string Unit)> items)
    {
        return string.Join(" · ", items
            .GroupBy(i => i.Unit)
            .Select(g => $"{g.Sum(i => i.Quantity)} {g.Key}"));
    }

    public static string Products(int count) => count == 1 ? "1 producto" : $"{count} productos";
}
