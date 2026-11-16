using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Genera y consulta datos históricos de ventas de prueba (HU-17).
/// Los datos se guardan en tablas propias: no crean ventas, no cambian existencias
/// ni registran movimientos de inventario.
/// </summary>
public class SyntheticSalesService : ISyntheticSalesService
{
    private readonly ISyntheticSalesRepository _syntheticRepository;
    private readonly IProductRepository _productRepository;
    private readonly SyntheticDataOptions _options;
    private readonly TimeProvider _timeProvider;

    public SyntheticSalesService(
        ISyntheticSalesRepository syntheticRepository,
        IProductRepository productRepository,
        SyntheticDataOptions options,
        TimeProvider timeProvider)
    {
        _syntheticRepository = syntheticRepository;
        _productRepository = productRepository;
        _options = options;
        _timeProvider = timeProvider;
    }

    public bool IsGenerationEnabled => _options.Enabled;

    public async Task<HistoricalSalesGenerationResultDto> GenerateAsync(HistoricalSalesGenerationDto request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return HistoricalSalesGenerationResultDto.Failure("La generación de datos de prueba está deshabilitada. Solo puede usarse en entornos de desarrollo o pruebas.");

        // Se validan todos los parámetros antes de generar nada.
        var error = ValidateParameters(request);
        if (error is not null)
            return HistoricalSalesGenerationResultDto.Failure(error);

        var startDate = request.StartDate!.Value.Date;
        var endDate = request.EndDate!.Value.Date;
        var productIds = request.ProductIds.Distinct().OrderBy(id => id).ToList();

        var products = new List<Product>();
        foreach (var id in productIds)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product is null)
                return HistoricalSalesGenerationResultDto.Failure("Uno o más productos seleccionados no existen. Actualiza la página y vuelve a seleccionarlos.");
            products.Add(product);
        }

        // Evita cargar dos veces el mismo lote por error.
        var parametersKey = BuildParametersKey(startDate, endDate, request.DemandPattern, request.RandomSeed, productIds);
        var existing = await _syntheticRepository.GetBatchByKeyAsync(parametersKey, cancellationToken);
        if (existing is not null)
            return HistoricalSalesGenerationResultDto.Failure($"Ya existe un lote con los mismos parámetros (lote #{existing.Id}). No se volvió a cargar.");

        // Tampoco se permiten datos superpuestos de un mismo producto: se sumarían dos veces en el análisis.
        var overlapping = await _syntheticRepository.GetProductsWithDataAsync(productIds, startDate, endDate, cancellationToken);
        if (overlapping.Count > 0)
        {
            var names = string.Join(", ", products.Where(p => overlapping.Contains(p.Id)).Select(p => $"«{p.Name}»"));
            return HistoricalSalesGenerationResultDto.Failure($"Ya hay datos de prueba para {names} en ese período. Elimina el lote anterior o elige otras fechas.");
        }

        var batch = new SyntheticSalesBatch
        {
            CreatedAt = _timeProvider.GetLocalNow().DateTime,
            StartDate = startDate,
            EndDate = endDate,
            Pattern = request.DemandPattern,
            RandomSeed = request.RandomSeed,
            ProductIds = string.Join(",", productIds),
            ProductCount = productIds.Count,
            ParametersKey = parametersKey
        };

        foreach (var product in products)
        {
            foreach (var record in GenerateRecords(product, startDate, endDate, request.DemandPattern, request.RandomSeed))
                batch.Records.Add(record);
        }

        batch.RecordCount = batch.Records.Count;

        try
        {
            // El lote y todos sus registros se guardan juntos: si algo falla, no queda nada a medias.
            await _syntheticRepository.AddBatchAsync(batch, cancellationToken);
        }
        catch (BusinessException ex)
        {
            return HistoricalSalesGenerationResultDto.Failure(ex.Message);
        }

        return new HistoricalSalesGenerationResultDto
        {
            BatchId = batch.Id,
            GeneratedRecords = batch.RecordCount,
            ProductCount = batch.ProductCount,
            StartDate = startDate,
            EndDate = endDate,
            IsSuccessful = true
        };
    }

    private string? ValidateParameters(HistoricalSalesGenerationDto request)
    {
        var today = _timeProvider.GetLocalNow().Date;

        if (request.StartDate is null || request.EndDate is null)
            return "Indica la fecha inicial y la fecha final del período.";

        var startDate = request.StartDate.Value.Date;
        var endDate = request.EndDate.Value.Date;

        if (startDate > endDate)
            return "La fecha inicial no puede ser posterior a la fecha final. Revisa el período seleccionado.";

        if (endDate > today)
            return "No se pueden generar datos con fechas futuras. La fecha final debe ser hoy o una fecha anterior.";

        var days = (endDate - startDate).Days + 1;
        if (days > _options.MaxDays)
            return $"El período no puede superar los {_options.MaxDays} días (24 meses). Acorta el rango de fechas.";

        if (!Enum.IsDefined(request.DemandPattern))
            return "Selecciona un patrón de demanda válido.";

        if (request.RandomSeed < 0)
            return "La semilla debe ser un número entero igual o mayor que cero.";

        var productIds = request.ProductIds.Distinct().ToList();
        if (productIds.Count == 0)
            return "Selecciona al menos un producto.";

        if (productIds.Any(id => id <= 0))
            return "Uno o más productos seleccionados no son válidos.";

        if (productIds.Count > _options.MaxProductsPerBatch)
            return $"Puedes generar datos para un máximo de {_options.MaxProductsPerBatch} productos por vez.";

        // Como máximo un registro por producto y día.
        var maxRecords = (long)days * productIds.Count;
        if (maxRecords > _options.MaxRecordsPerBatch)
            return $"La generación podría crear hasta {maxRecords:N0} registros y el máximo por lote es {_options.MaxRecordsPerBatch:N0}. Reduce el período o la cantidad de productos.";

        return null;
    }

    /// <summary>
    /// Genera la cantidad diaria de un producto. Todo depende de la semilla y del producto,
    /// por lo que la misma configuración produce siempre los mismos datos.
    /// </summary>
    internal static List<SyntheticSalesRecord> GenerateRecords(Product product, DateTime startDate, DateTime endDate, DemandPattern pattern, int seed)
    {
        // Cada producto tiene su propio generador para que no todos se comporten igual.
        var random = new Random(unchecked(seed * 7919 + product.Id));

        var (minLevel, maxLevel) = GetDailyLevelRange(product.UnitOfMeasure);
        var baseLevel = minLevel + random.NextDouble() * (maxLevel - minLevel);
        var noise = 0.15 + random.NextDouble() * 0.20;          // variación diaria entre 15 % y 35 %
        var weekendFactor = 1.0 + random.NextDouble() * 0.30;   // fines de semana algo más altos
        var seasonPhase = random.NextDouble() * 2 * Math.PI;    // cada producto tiene su propia temporada

        var totalDays = (endDate - startDate).Days + 1;
        var records = new List<SyntheticSalesRecord>();

        for (var day = 0; day < totalDays; day++)
        {
            var date = startDate.AddDays(day);
            var progress = totalDays == 1 ? 0 : (double)day / (totalDays - 1);

            var patternFactor = pattern switch
            {
                DemandPattern.Increasing => 0.6 + 0.8 * progress,   // del 60 % al 140 %
                DemandPattern.Decreasing => 1.4 - 0.8 * progress,   // del 140 % al 60 %
                DemandPattern.Seasonal => 1.0 + 0.4 * Math.Sin(2 * Math.PI * date.DayOfYear / 365.25 + seasonPhase),
                _ => 1.0
            };

            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var expected = baseLevel * patternFactor * (isWeekend ? weekendFactor : 1.0);
            var value = expected * (1 + noise * (2 * random.NextDouble() - 1));

            // Las cantidades son enteras, igual que en las ventas reales de cualquier unidad de medida.
            var quantity = (int)Math.Round(value, MidpointRounding.AwayFromZero);

            // Un día con cantidad 0 no se guarda: dentro del período del lote significa "sin ventas".
            if (quantity > 0)
                records.Add(new SyntheticSalesRecord { ProductId = product.Id, Date = date, Quantity = quantity });
        }

        return records;
    }

    /// <summary>
    /// Rango de ventas diarias promedio según la unidad de medida:
    /// se venden menos cajas que unidades sueltas.
    /// </summary>
    private static (double Min, double Max) GetDailyLevelRange(string unitOfMeasure)
    {
        return unitOfMeasure.Trim().ToUpperInvariant() switch
        {
            "CAJA" => (1, 3),
            "KG" or "LT" => (2, 8),
            _ => (3, 12)
        };
    }

    private static string BuildParametersKey(DateTime startDate, DateTime endDate, DemandPattern pattern, int seed, IEnumerable<int> productIds)
        => $"{startDate:yyyyMMdd}|{endDate:yyyyMMdd}|{pattern}|{seed}|{string.Join(",", productIds)}";

    public async Task<IReadOnlyList<SyntheticSalesBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        var batches = await _syntheticRepository.GetBatchesAsync(cancellationToken);
        var products = (await _productRepository.GetAllAsync(cancellationToken)).ToDictionary(p => p.Id, p => p.Name);

        return batches.Select(b => new SyntheticSalesBatchDto
        {
            Id = b.Id,
            CreatedAt = b.CreatedAt,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            Pattern = b.Pattern,
            RandomSeed = b.RandomSeed,
            ProductCount = b.ProductCount,
            RecordCount = b.RecordCount,
            ProductNames = b.ProductIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var value) && products.TryGetValue(value, out var name) ? name : $"Producto {id}")
                .ToList()
        }).ToList();
    }

    public async Task<bool> DeleteBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return false;

        return await _syntheticRepository.DeleteBatchAsync(batchId, cancellationToken);
    }

    public async Task<IReadOnlyList<HistoricalSalesRecordDto>> GetRecordsAsync(int? productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        if (fromDate.Date > toDate.Date)
            throw new BusinessException("La fecha inicial no puede ser posterior a la fecha final. Revisa el período seleccionado.");

        var records = await _syntheticRepository.GetRecordsAsync(productId, fromDate.Date, toDate.Date, cancellationToken);

        return records.Select(r => new HistoricalSalesRecordDto
        {
            ProductId = r.ProductId,
            Date = r.Date,
            Quantity = r.Quantity
        }).ToList();
    }
}
