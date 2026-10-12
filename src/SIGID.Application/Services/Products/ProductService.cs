using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;

namespace SIGID.Application.Services.Products;

/// <summary>
/// Lógica de negocio de productos (HU-05).
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Where(p => p.IsActive).Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : MapToDto(product);
    }

    public async Task<int> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(dto, current: null, cancellationToken);

        var product = new Product();
        MapToEntity(dto, product);

        // Un producto nuevo empieza sin existencias; solo las entradas de inventario las aumentan.
        product.CurrentStock = 0;

        await _productRepository.AddAsync(product, cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new BusinessException("El producto que intenta editar no existe.");

        await ValidateAsync(dto, product, cancellationToken);

        // CurrentStock no se toca aquí: solo cambia con movimientos de inventario.
        MapToEntity(dto, product);
        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Validaciones de negocio
    // ------------------------------------------------------------------
    private async Task ValidateAsync(ProductDto dto, Product? current, CancellationToken cancellationToken)
    {
        var sku = dto.SKU?.Trim() ?? string.Empty;
        var name = dto.Name?.Trim() ?? string.Empty;
        var unit = dto.UnitOfMeasure?.Trim() ?? string.Empty;

        if (sku.Length == 0)
            throw new BusinessException("El SKU es obligatorio.", nameof(ProductDto.SKU));

        if (sku.Length > 50)
            throw new BusinessException("El SKU no puede superar los 50 caracteres.", nameof(ProductDto.SKU));

        if (name.Length == 0)
            throw new BusinessException("El nombre es obligatorio.", nameof(ProductDto.Name));

        if (name.Length > 150)
            throw new BusinessException("El nombre no puede superar los 150 caracteres.", nameof(ProductDto.Name));

        if (dto.Description?.Trim().Length > 500)
            throw new BusinessException("La descripción no puede superar los 500 caracteres.", nameof(ProductDto.Description));

        if (unit.Length == 0)
            throw new BusinessException("La unidad de medida es obligatoria.", nameof(ProductDto.UnitOfMeasure));

        if (unit.Length > 20)
            throw new BusinessException("La unidad de medida no puede superar los 20 caracteres.", nameof(ProductDto.UnitOfMeasure));

        if (dto.MinimumStock < 0)
            throw new BusinessException("El stock mínimo no puede ser negativo.", nameof(ProductDto.MinimumStock));

        if (dto.MaximumStock < 0)
            throw new BusinessException("El stock máximo no puede ser negativo.", nameof(ProductDto.MaximumStock));

        if (dto.MaximumStock < dto.MinimumStock)
            throw new BusinessException("El stock máximo debe ser mayor o igual al stock mínimo.", nameof(ProductDto.MaximumStock));

        if (await _productRepository.ExistsBySkuAsync(sku.ToUpperInvariant(), current?.Id, cancellationToken))
            throw new BusinessException($"Ya existe un producto con el SKU '{sku.ToUpperInvariant()}'.", nameof(ProductDto.SKU));

        // La categoría y el proveedor deben existir. Si se asignan por primera vez
        // (o se cambian) también deben estar activos.
        var category = dto.CategoryId > 0
            ? await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken)
            : null;

        if (category is null)
            throw new BusinessException("Seleccione una categoría existente.", nameof(ProductDto.CategoryId));

        if (!category.IsActive && current?.CategoryId != category.Id)
            throw new BusinessException("La categoría seleccionada está inactiva.", nameof(ProductDto.CategoryId));

        var supplier = dto.SupplierId > 0
            ? await _supplierRepository.GetByIdAsync(dto.SupplierId, cancellationToken)
            : null;

        if (supplier is null)
            throw new BusinessException("Seleccione un proveedor existente.", nameof(ProductDto.SupplierId));

        if (!supplier.IsActive && current?.SupplierId != supplier.Id)
            throw new BusinessException("El proveedor seleccionado está inactivo.", nameof(ProductDto.SupplierId));
    }

    // ------------------------------------------------------------------
    // Mapeo manual (sin AutoMapper)
    // ------------------------------------------------------------------
    internal static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        SKU = product.SKU,
        Name = product.Name,
        Description = product.Description,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        SupplierId = product.SupplierId,
        SupplierName = product.Supplier?.Name,
        UnitOfMeasure = product.UnitOfMeasure,
        MinimumStock = product.MinimumStock,
        MaximumStock = product.MaximumStock,
        CurrentStock = product.CurrentStock,
        StockStatus = product.GetStockStatus(),
        IsActive = product.IsActive
    };

    private static void MapToEntity(ProductDto dto, Product product)
    {
        product.SKU = dto.SKU.Trim().ToUpperInvariant();
        product.Name = dto.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;
        product.UnitOfMeasure = dto.UnitOfMeasure.Trim().ToUpperInvariant();
        product.MinimumStock = dto.MinimumStock;
        product.MaximumStock = dto.MaximumStock;
        product.IsActive = dto.IsActive;
    }
}
