using SIGID.Application.DTOs.Sales;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Persistence;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;

namespace SIGID.Application.Services.Sales;

/// <summary>
/// Registro de ventas (HU-14), actualización del inventario por ventas (HU-15)
/// e historial de ventas (HU-16).
/// </summary>
public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ITransactionRunner _transactionRunner;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        ITransactionRunner transactionRunner)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _transactionRunner = transactionRunner;
    }

    // ------------------------------------------------------------------
    // Registro de ventas
    // ------------------------------------------------------------------
    public async Task<SaleRegistrationResultDto> RegisterAsync(RegisterSaleDto dto, CancellationToken cancellationToken = default)
    {
        var requestId = dto.RequestId == Guid.Empty ? Guid.NewGuid() : dto.RequestId;

        // Si este formulario ya se registró (doble clic o reenvío), se devuelve la venta existente.
        var existing = await _saleRepository.GetByRequestIdAsync(requestId, cancellationToken);
        if (existing is not null)
            return SaleRegistrationResultDto.Success(await BuildSaleDtoAsync(existing.Id, cancellationToken), wasAlreadyRegistered: true);

        var items = dto.Details ?? new List<RegisterSaleDetailDto>();

        // Todas las validaciones se hacen antes de guardar: si un producto falla, se rechaza la venta completa.
        var errors = await ValidateAsync(items, cancellationToken);
        if (errors.Count > 0)
            return SaleRegistrationResultDto.Failure(errors);

        try
        {
            // Venta + detalles + descuento de existencias + movimientos: todo o nada.
            var saleId = await _transactionRunner.ExecuteAsync(async () =>
            {
                var sale = new Sale
                {
                    Date = DateTime.Now,
                    RequestId = requestId,
                    Details = items
                        .Select(i => new SaleDetail { ProductId = i.ProductId, Quantity = i.Quantity })
                        .ToList()
                };

                await _saleRepository.AddAsync(sale, cancellationToken);

                // Reutiliza las reglas de salidas del Sprint 2. Si otra operación consumió
                // las existencias mientras tanto, lanza una excepción y se revierte todo.
                await _inventoryService.RegisterSaleExitsAsync(sale.Id, sale.Date, items, cancellationToken);

                return sale.Id;
            }, cancellationToken);

            return SaleRegistrationResultDto.Success(await BuildSaleDtoAsync(saleId, cancellationToken));
        }
        catch (BusinessException)
        {
            // Pudo ser un envío duplicado que se guardó al mismo tiempo.
            var duplicated = await _saleRepository.GetByRequestIdAsync(requestId, cancellationToken);
            if (duplicated is not null)
                return SaleRegistrationResultDto.Success(await BuildSaleDtoAsync(duplicated.Id, cancellationToken), wasAlreadyRegistered: true);

            // Lo más probable: las existencias cambiaron. Se vuelven a calcular para indicar qué producto falla.
            var stockErrors = await ValidateStockAsync(items, cancellationToken);
            return stockErrors.Count > 0
                ? SaleRegistrationResultDto.Failure(stockErrors)
                : SaleRegistrationResultDto.Failure("No se pudo registrar la venta y no se guardó ningún cambio. Intenta nuevamente.");
        }
    }

    private async Task<List<SaleErrorDto>> ValidateAsync(List<RegisterSaleDetailDto> items, CancellationToken cancellationToken)
    {
        var errors = new List<SaleErrorDto>();

        if (items.Count == 0)
        {
            errors.Add(new SaleErrorDto { Message = "Agrega al menos un producto antes de confirmar la venta." });
            return errors;
        }

        if (items.Any(i => i.ProductId <= 0))
            errors.Add(new SaleErrorDto { Message = "Hay una línea sin producto. Selecciona un producto o quítala de la venta." });

        // Un producto solo puede aparecer una vez; en la pantalla se suma la cantidad al volver a agregarlo.
        foreach (var repeated in items.Where(i => i.ProductId > 0).GroupBy(i => i.ProductId).Where(g => g.Count() > 1))
            errors.Add(new SaleErrorDto { ProductId = repeated.Key, Message = "Un producto aparece más de una vez en la venta. Indica una sola cantidad total para ese producto." });

        foreach (var item in items.Where(i => i.ProductId > 0).DistinctBy(i => i.ProductId))
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is null)
            {
                errors.Add(new SaleErrorDto { ProductId = item.ProductId, Message = "Uno de los productos seleccionados ya no existe. Quítalo de la venta." });
                continue;
            }

            if (!product.IsActive)
                errors.Add(new SaleErrorDto { ProductId = item.ProductId, Message = $"«{product.Name}» está inactivo y no se puede vender. Quítalo de la venta." });
            else if (item.Quantity <= 0)
                errors.Add(new SaleErrorDto { ProductId = item.ProductId, Message = $"La cantidad de «{product.Name}» debe ser mayor que cero." });
            else if (item.Quantity > product.CurrentStock)
                errors.Add(StockError(product, item.Quantity, product.CurrentStock));
        }

        return errors;
    }

    // Consulta las existencias actuales directamente (sin datos en caché) para explicar un rechazo.
    private async Task<List<SaleErrorDto>> ValidateStockAsync(List<RegisterSaleDetailDto> items, CancellationToken cancellationToken)
    {
        var errors = new List<SaleErrorDto>();

        foreach (var item in items)
        {
            var available = await _inventoryService.GetCurrentStockAsync(item.ProductId, cancellationToken) ?? 0;
            if (item.Quantity > available)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product is not null)
                    errors.Add(StockError(product, item.Quantity, available));
            }
        }

        return errors;
    }

    // Mensaje de existencias insuficientes al confirmar la venta.
    private static SaleErrorDto StockError(Product product, int requested, int available) => new()
    {
        ProductId = product.Id,
        IsStockError = true,
        Message = available <= 0
            ? $"«{product.Name}» ya no tiene existencias disponibles. Quítalo de la venta."
            : $"Pediste {requested} {product.UnitOfMeasure} de «{product.Name}», pero solamente hay {available} {product.UnitOfMeasure} disponibles."
    };

    // ------------------------------------------------------------------
    // Comprobación al agregar o modificar un producto (antes de confirmar)
    // ------------------------------------------------------------------
    public async Task<SaleErrorDto?> CheckAvailabilityAsync(int productId, int quantity, int alreadyInSale = 0, bool isExistingLine = false, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

        if (product is null || !product.IsActive)
            return new SaleErrorDto { ProductId = productId, Message = "Selecciona un producto activo del catálogo." };

        if (quantity <= 0)
            return new SaleErrorDto { ProductId = productId, Message = $"Escribe una cantidad mayor que cero para «{product.Name}»." };

        // Se consulta la existencia actual en la base de datos.
        var available = await _inventoryService.GetCurrentStockAsync(productId, cancellationToken) ?? 0;
        var total = alreadyInSale + quantity;
        if (total <= available)
            return null;

        var unit = product.UnitOfMeasure;
        string message;
        if (available <= 0)
            message = $"«{product.Name}» no tiene existencias disponibles en este momento.";
        else if (isExistingLine)
            message = $"Indicaste {quantity} {unit} de «{product.Name}», pero solamente hay {available} {unit} disponibles.";
        else if (alreadyInSale > 0)
            message = $"Ya tienes {alreadyInSale} {unit} de «{product.Name}» en la venta. No puedes agregar {quantity} {unit} más porque solamente hay {available} {unit} disponibles.";
        else
            message = $"No puedes agregar {quantity} {unit} de «{product.Name}» porque solamente hay {available} {unit} disponibles.";

        return new SaleErrorDto { ProductId = productId, IsStockError = true, Message = message };
    }

    private async Task<SaleDto> BuildSaleDtoAsync(int saleId, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId, cancellationToken)
            ?? throw new BusinessException("La venta no existe.");

        var details = new List<SaleDetailDto>();
        foreach (var detail in sale.Details.OrderBy(d => d.Product?.Name))
        {
            details.Add(new SaleDetailDto
            {
                ProductId = detail.ProductId,
                ProductName = detail.Product?.Name ?? string.Empty,
                Sku = detail.Product?.SKU ?? string.Empty,
                UnitOfMeasure = detail.Product?.UnitOfMeasure ?? string.Empty,
                Quantity = detail.Quantity,
                StockAfter = await _inventoryService.GetCurrentStockAsync(detail.ProductId, cancellationToken) ?? 0
            });
        }

        return new SaleDto { Id = sale.Id, Date = sale.Date, Details = details };
    }

    // ------------------------------------------------------------------
    // Historial (solo lectura)
    // ------------------------------------------------------------------
    public async Task<IReadOnlyList<SaleHistoryDto>> GetHistoryAsync(SaleHistoryFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value.Date > filter.EndDate.Value.Date)
            throw new BusinessException("La fecha inicial no puede ser posterior a la fecha final.", nameof(SaleHistoryFilterDto.StartDate));

        // Las fechas incluyen el día completo.
        DateTime? from = filter.StartDate?.Date;
        DateTime? to = filter.EndDate?.Date.AddDays(1).AddTicks(-1);

        var sales = await _saleRepository.SearchAsync(filter.ProductId, from, to, cancellationToken);

        return sales.Select(s => new SaleHistoryDto
        {
            Id = s.Id,
            Date = s.Date,
            ProductCount = s.Details.Count,
            TotalQuantity = s.Details.Sum(d => d.Quantity),
            Items = s.Details
                .OrderBy(d => d.Product?.Name)
                .Select(d => new SaleHistoryItemDto
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? string.Empty,
                    SKU = d.Product?.SKU ?? string.Empty,
                    UnitOfMeasure = d.Product?.UnitOfMeasure ?? string.Empty,
                    Quantity = d.Quantity
                })
                .ToList()
        }).ToList();
    }

    public async Task<SaleHistoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        if (sale is null)
            return null;

        return new SaleHistoryDetailDto
        {
            Id = sale.Id,
            Date = sale.Date,
            Details = sale.Details
                .OrderBy(d => d.Product?.Name)
                .Select(d => new SaleHistoryItemDto
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? string.Empty,
                    SKU = d.Product?.SKU ?? string.Empty,
                    UnitOfMeasure = d.Product?.UnitOfMeasure ?? string.Empty,
                    Quantity = d.Quantity
                })
                .ToList()
        };
    }

    public Task<IReadOnlyList<ProductSalesByDayDto>> GetSoldQuantitiesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        if (fromDate.Date > toDate.Date)
            throw new BusinessException("La fecha inicial no puede ser posterior a la fecha final.");

        return _saleRepository.GetSoldQuantitiesByDayAsync(fromDate.Date, toDate.Date.AddDays(1).AddTicks(-1), cancellationToken);
    }
}
