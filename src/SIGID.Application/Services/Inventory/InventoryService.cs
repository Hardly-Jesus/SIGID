using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Sales;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Services.Inventory;

/// <summary>
/// Lógica de negocio de entradas, salidas e historial de inventario (HU-08, HU-09 y HU-10).
/// También procesa las salidas generadas por las ventas (HU-15) con las mismas reglas.
/// </summary>
public class InventoryService : IInventoryService
{
    private const int MaxQuantity = 1_000_000;
    private static readonly DateTime MinDate = new(2000, 1, 1);

    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;

    public InventoryService(IInventoryMovementRepository movementRepository, IProductRepository productRepository)
    {
        _movementRepository = movementRepository;
        _productRepository = productRepository;
    }

    public Task<InventoryMovementDto> RegisterEntryAsync(RegisterMovementDto entry, CancellationToken cancellationToken = default)
        => RegisterAsync(entry, MovementType.Entry, saleId: null, cancellationToken);

    public Task<InventoryMovementDto> RegisterExitAsync(RegisterMovementDto exit, CancellationToken cancellationToken = default)
        => RegisterAsync(exit, MovementType.Exit, saleId: null, cancellationToken);

    public async Task<IReadOnlyList<InventoryMovementDto>> RegisterSaleExitsAsync(
        int saleId, DateTime date, IReadOnlyList<RegisterSaleDetailDto> items, CancellationToken cancellationToken = default)
    {
        if (saleId <= 0)
            throw new BusinessException("La venta debe guardarse antes de registrar sus salidas de inventario.");

        var movements = new List<InventoryMovementDto>();

        // Se procesan en orden de producto para que dos ventas simultáneas bloqueen las filas
        // en el mismo orden y no se queden esperando una a la otra.
        foreach (var item in items.OrderBy(i => i.ProductId))
        {
            var exit = new RegisterMovementDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Date = date,
                Description = $"Venta #{saleId}"
            };

            movements.Add(await RegisterAsync(exit, MovementType.Exit, saleId, cancellationToken));
        }

        return movements;
    }

    public Task<int?> GetCurrentStockAsync(int productId, CancellationToken cancellationToken = default)
        => _movementRepository.GetCurrentStockAsync(productId, cancellationToken);

    public async Task<IReadOnlyList<InventoryMovementDto>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var movements = await _movementRepository.GetByTypeAsync(MovementType.Entry, cancellationToken);
        return movements.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetExitsAsync(CancellationToken cancellationToken = default)
    {
        var movements = await _movementRepository.GetByTypeAsync(MovementType.Exit, cancellationToken);
        return movements.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetHistoryAsync(MovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value.Date > filter.ToDate.Value.Date)
            throw new BusinessException("La fecha inicial no puede ser posterior a la fecha final.", nameof(MovementFilterDto.FromDate));

        // Los filtros de fecha toman el día completo: desde las 00:00 del día inicial
        // hasta el último instante del día final.
        DateTime? from = filter.FromDate?.Date;
        DateTime? to = filter.ToDate?.Date.AddDays(1).AddTicks(-1);

        var movements = await _movementRepository.SearchAsync(
            filter.ProductId,
            filter.MovementType,
            from,
            to,
            cancellationToken);

        return movements.Select(MapToDto).ToList();
    }

    // ------------------------------------------------------------------
    // Registro de movimientos
    // ------------------------------------------------------------------
    private async Task<InventoryMovementDto> RegisterAsync(RegisterMovementDto dto, MovementType type, int? saleId, CancellationToken cancellationToken)
    {
        if (dto.ProductId <= 0)
            throw new BusinessException("Seleccione un producto.", nameof(RegisterMovementDto.ProductId));

        if (dto.Quantity <= 0)
            throw new BusinessException("La cantidad debe ser mayor que cero.", nameof(RegisterMovementDto.Quantity));

        if (dto.Quantity > MaxQuantity)
            throw new BusinessException($"La cantidad no puede superar {MaxQuantity:N0} unidades por movimiento.", nameof(RegisterMovementDto.Quantity));

        ValidateDate(dto.Date);

        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        if (description?.Length > 250)
            throw new BusinessException("La observación no puede superar los 250 caracteres.", nameof(RegisterMovementDto.Description));

        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken)
            ?? throw new BusinessException("El producto seleccionado no existe.", nameof(RegisterMovementDto.ProductId));

        if (!product.IsActive)
            throw new BusinessException("El producto seleccionado está inactivo y no admite movimientos.", nameof(RegisterMovementDto.ProductId));

        // Regla de salidas: no se puede retirar más de lo que hay disponible.
        if (type == MovementType.Exit && dto.Quantity > product.CurrentStock)
            throw new BusinessException(InsufficientStockMessage(product.CurrentStock, product.UnitOfMeasure), nameof(RegisterMovementDto.Quantity));

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            MovementType = type,
            Quantity = dto.Quantity,
            Date = dto.Date,
            Description = description,
            SaleId = saleId
        };

        // El repositorio actualiza las existencias y guarda el movimiento en una sola transacción.
        // Si otra salida consumió las existencias justo antes, devuelve false.
        var registered = await _movementRepository.RegisterAsync(movement, cancellationToken);
        if (!registered)
        {
            var available = await _movementRepository.GetCurrentStockAsync(product.Id, cancellationToken) ?? 0;
            throw new BusinessException(InsufficientStockMessage(available, product.UnitOfMeasure), nameof(RegisterMovementDto.Quantity));
        }

        movement.Product = product;
        return MapToDto(movement);
    }

    private static void ValidateDate(DateTime date)
    {
        if (date == default)
            throw new BusinessException("La fecha es obligatoria.", nameof(RegisterMovementDto.Date));

        if (date < MinDate)
            throw new BusinessException("La fecha no es válida.", nameof(RegisterMovementDto.Date));

        // Se deja un pequeño margen por diferencias de reloj entre el navegador y el servidor.
        if (date > DateTime.Now.AddMinutes(5))
            throw new BusinessException("La fecha no puede ser posterior a la fecha y hora actual.", nameof(RegisterMovementDto.Date));
    }

    private static string InsufficientStockMessage(int available, string unit) =>
        $"Existencias insuficientes. Disponible: {available} {unit}.";

    internal static InventoryMovementDto MapToDto(InventoryMovement movement) => new()
    {
        Id = movement.Id,
        ProductId = movement.ProductId,
        ProductSku = movement.Product?.SKU ?? string.Empty,
        ProductName = movement.Product?.Name ?? string.Empty,
        UnitOfMeasure = movement.Product?.UnitOfMeasure ?? string.Empty,
        MovementType = movement.MovementType,
        Quantity = movement.Quantity,
        Date = movement.Date,
        Description = movement.Description,
        StockAfter = movement.StockAfter,
        SaleId = movement.SaleId
    };
}
