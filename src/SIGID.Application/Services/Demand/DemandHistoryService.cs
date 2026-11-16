using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Arma la serie diaria de ventas de un producto, usada por el análisis (HU-18),
/// la predicción (HU-19) y la evaluación (HU-21).
/// </summary>
/// <remarks>
/// Diferencia dos casos que no son lo mismo:
/// - Día conocido sin ventas: vale 0.
/// - Día sin información: no se convierte en 0 y se informa aparte.
/// Ventas reales: hay información desde la primera venta registrada del producto hasta hoy.
/// Datos de prueba: hay información en los períodos de los lotes que incluyen al producto.
/// </remarks>
public class DemandHistoryService : IDemandHistoryService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISyntheticSalesRepository _syntheticRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    public DemandHistoryService(
        ISaleRepository saleRepository,
        ISyntheticSalesRepository syntheticRepository,
        IProductRepository productRepository,
        TimeProvider timeProvider)
    {
        _saleRepository = saleRepository;
        _syntheticRepository = syntheticRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DemandSeriesDto> GetDailySeriesAsync(int productId, SalesDataSource dataSource, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new BusinessException("El producto seleccionado no existe.");

        if (!Enum.IsDefined(dataSource))
            throw new BusinessException("Selecciona un origen de datos válido.");

        fromDate = fromDate.Date;
        toDate = toDate.Date;

        var series = new DemandSeriesDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitOfMeasure = product.UnitOfMeasure,
            DataSource = dataSource
        };

        if (fromDate > toDate)
            return series;

        // Períodos con información conocida, recortados al rango pedido.
        var coverage = (await GetCoverageAsync(productId, dataSource, cancellationToken))
            .Select(c => (Start: c.StartDate > fromDate ? c.StartDate : fromDate, End: c.EndDate < toDate ? c.EndDate : toDate))
            .Where(c => c.Start <= c.End)
            .OrderBy(c => c.Start)
            .ToList();

        if (coverage.Count == 0)
            return series;

        var start = coverage.First().Start;
        var end = coverage.Max(c => c.End);

        // Cada fuente se consulta por separado: los datos reales y los de prueba nunca se mezclan.
        var quantities = dataSource == SalesDataSource.Real
            ? await _saleRepository.GetDailyQuantitiesAsync(productId, start, end, cancellationToken)
            : await _syntheticRepository.GetDailyQuantitiesAsync(productId, start, end, cancellationToken);

        // Recorremos día por día en orden cronológico.
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var isKnown = coverage.Any(c => date >= c.Start && date <= c.End);
            if (!isKnown)
            {
                series.UnknownDayCount++;
                continue;
            }

            var quantity = quantities.TryGetValue(date, out var value) ? value : 0;
            if (quantity < 0)
            {
                // Una cantidad negativa no es válida: se descarta y se informa.
                series.InvalidRecordCount++;
                continue;
            }

            series.Points.Add(new DailyDemandPointDto { Date = date, Quantity = quantity });
        }

        series.StartDate = series.Points.FirstOrDefault()?.Date;
        series.EndDate = series.Points.LastOrDefault()?.Date;
        return series;
    }

    private async Task<IReadOnlyList<(DateTime StartDate, DateTime EndDate)>> GetCoverageAsync(int productId, SalesDataSource dataSource, CancellationToken cancellationToken)
    {
        if (dataSource == SalesDataSource.Synthetic)
            return await _syntheticRepository.GetCoverageAsync(productId, cancellationToken);

        // Desde la primera venta registrada del producto, un día sin ventas es un 0 confirmado.
        // Antes de esa fecha no sabemos si el producto ya se vendía: esos días no se rellenan con 0.
        var firstSale = await _saleRepository.GetFirstSaleDateAsync(productId, cancellationToken);
        if (firstSale is null)
            return Array.Empty<(DateTime, DateTime)>();

        var today = _timeProvider.GetLocalNow().Date;
        return new[] { (firstSale.Value.Date, today) };
    }
}
