using System.Text.RegularExpressions;
using SIGID.Application.DTOs;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Entities;

namespace SIGID.Application.Services.Suppliers;

/// <summary>
/// Lógica de negocio de proveedores (HU-07).
/// </summary>
public class SupplierService : ISupplierService
{
    // Formatos sencillos para validar los datos de contacto.
    private static readonly Regex EmailFormat = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex PhoneFormat = new(@"^[0-9+()\-\s]{7,30}$");

    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
        var counts = await _supplierRepository.GetProductCountsAsync(cancellationToken);

        return suppliers
            .Select(s => MapToDto(s, counts.GetValueOrDefault(s.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<SupplierDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(s => s.IsActive).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier is null)
            return null;

        var counts = await _supplierRepository.GetProductCountsAsync(cancellationToken);
        return MapToDto(supplier, counts.GetValueOrDefault(supplier.Id));
    }

    public async Task<int> CreateAsync(SupplierDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(dto, excludeId: null, cancellationToken);

        var supplier = new Supplier();
        MapToEntity(dto, supplier);

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        return supplier.Id;
    }

    public async Task UpdateAsync(SupplierDto dto, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new BusinessException("El proveedor que intenta editar no existe.");

        await ValidateAsync(dto, excludeId: dto.Id, cancellationToken);

        MapToEntity(dto, supplier);
        await _supplierRepository.UpdateAsync(supplier, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessException("El proveedor que intenta eliminar no existe.");

        // Regla: no se puede eliminar un proveedor que tenga productos asociados.
        if (await _supplierRepository.HasProductsAsync(id, cancellationToken))
            throw new BusinessException(
                $"No se puede eliminar el proveedor '{supplier.Name}' porque tiene productos asociados. " +
                "Puede marcarlo como inactivo.");

        await _supplierRepository.DeleteAsync(supplier, cancellationToken);
    }

    private async Task ValidateAsync(SupplierDto dto, int? excludeId, CancellationToken cancellationToken)
    {
        var name = dto.Name?.Trim() ?? string.Empty;
        var identification = Clean(dto.Identification);
        var phone = Clean(dto.Phone);
        var email = Clean(dto.Email);
        var address = Clean(dto.Address);

        if (name.Length == 0)
            throw new BusinessException("El nombre del proveedor es obligatorio.", nameof(SupplierDto.Name));

        if (name.Length > 150)
            throw new BusinessException("El nombre no puede superar los 150 caracteres.", nameof(SupplierDto.Name));

        if (identification?.Length > 30)
            throw new BusinessException("La identificación/RNC no puede superar los 30 caracteres.", nameof(SupplierDto.Identification));

        if (phone is not null && !PhoneFormat.IsMatch(phone))
            throw new BusinessException("El teléfono solo admite números, espacios, +, - y paréntesis (mínimo 7 caracteres).", nameof(SupplierDto.Phone));

        if (email is not null && (email.Length > 150 || !EmailFormat.IsMatch(email)))
            throw new BusinessException("El correo electrónico no tiene un formato válido.", nameof(SupplierDto.Email));

        if (address?.Length > 250)
            throw new BusinessException("La dirección no puede superar los 250 caracteres.", nameof(SupplierDto.Address));

        // La identificación es opcional, pero si se indica no puede repetirse.
        if (identification is not null)
        {
            var existing = await _supplierRepository.GetByIdentificationAsync(identification, cancellationToken);
            if (existing is not null && existing.Id != excludeId)
                throw new BusinessException(
                    $"Ya existe un proveedor con la identificación/RNC '{identification}' ({existing.Name}).",
                    nameof(SupplierDto.Identification));
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SupplierDto MapToDto(Supplier supplier, int productCount) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Identification = supplier.Identification,
        Phone = supplier.Phone,
        Email = supplier.Email,
        Address = supplier.Address,
        IsActive = supplier.IsActive,
        ProductCount = productCount
    };

    private static void MapToEntity(SupplierDto dto, Supplier supplier)
    {
        supplier.Name = dto.Name.Trim();
        supplier.Identification = Clean(dto.Identification);
        supplier.Phone = Clean(dto.Phone);
        supplier.Email = Clean(dto.Email)?.ToLowerInvariant();
        supplier.Address = Clean(dto.Address);
        supplier.IsActive = dto.IsActive;
    }
}
