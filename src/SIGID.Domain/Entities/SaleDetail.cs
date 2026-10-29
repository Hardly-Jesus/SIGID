namespace SIGID.Domain.Entities;

/// <summary>
/// Producto incluido en una venta y la cantidad vendida.
/// </summary>
public class SaleDetail
{
    public int Id { get; set; }

    public int SaleId { get; set; }

    public Sale? Sale { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; }
}
