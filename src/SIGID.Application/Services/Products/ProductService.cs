using SIGID.Application.DTOs;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;

namespace SIGID.Application.Services.Products;

/// <summary>
/// Lógica de negocio de productos (HU-05).
/// Contiene la estructura base y las validaciones principales; el flujo completo
/// se terminará de implementar y validar durante el Sprint 1.
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

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : MapToDto(product);
    }

    public async Task<int> CreateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(dto, excludeId: null, cancellationToken);

        var product = new Product();
        MapToEntity(dto, product);

        await _productRepository.AddAsync(product, cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(ProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new InvalidOperationException($"No existe el producto con Id {dto.Id}.");

        await ValidateAsync(dto, excludeId: dto.Id, cancellationToken);

        MapToEntity(dto, product);
        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Validaciones principales de negocio
    // ------------------------------------------------------------------
    private async Task ValidateAsync(ProductDto dto, int? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.SKU))
            throw new InvalidOperationException("El SKU es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.UnitOfMeasure))
            throw new InvalidOperationException("La unidad de medida es obligatoria.");

        if (dto.MinimumStock < 0 || dto.MaximumStock < 0)
            throw new InvalidOperationException("Los valores de stock no pueden ser negativos.");

        if (dto.MaximumStock < dto.MinimumStock)
            throw new InvalidOperationException("El stock máximo debe ser mayor o igual al stock mínimo.");

        if (await _productRepository.ExistsBySkuAsync(dto.SKU.Trim(), excludeId, cancellationToken))
            throw new InvalidOperationException($"Ya existe un producto con el SKU '{dto.SKU}'.");

        if (!await _categoryRepository.ExistsAsync(dto.CategoryId, cancellationToken))
            throw new InvalidOperationException("La categoría seleccionada no existe.");

        if (!await _supplierRepository.ExistsAsync(dto.SupplierId, cancellationToken))
            throw new InvalidOperationException("El proveedor seleccionado no existe.");
    }

    // ------------------------------------------------------------------
    // Mapeo manual (sin AutoMapper)
    // ------------------------------------------------------------------
    private static ProductDto MapToDto(Product product) => new()
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
