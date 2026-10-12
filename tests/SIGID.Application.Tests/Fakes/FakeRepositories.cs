using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests.Fakes;

public class FakeProductRepository : IProductRepository
{
    private readonly InMemoryDatabase _db;

    public FakeProductRepository(InMemoryDatabase db) => _db = db;

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            foreach (var p in _db.Products) _db.FillNavigation(p);
            IReadOnlyList<Product> result = _db.Products.OrderBy(p => p.Name).ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product is not null) _db.FillNavigation(product);
            return Task.FromResult(product);
        }
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Products.FirstOrDefault(p => string.Equals(p.SKU, sku.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Products.Any(p =>
                string.Equals(p.SKU, sku, StringComparison.OrdinalIgnoreCase) && (excludeId == null || p.Id != excludeId)));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            product.Id = _db.NextId();
            _db.Products.Add(product);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) _db.FillNavigation(product);
        return Task.CompletedTask;
    }
}

public class FakeCategoryRepository : ICategoryRepository
{
    private readonly InMemoryDatabase _db;

    public FakeCategoryRepository(InMemoryDatabase db) => _db = db;

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IReadOnlyList<Category> result = _db.Categories.OrderBy(c => c.Name).ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Categories.FirstOrDefault(c => c.Id == id));
    }

    public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Categories.FirstOrDefault(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Categories.Any(c => c.Id == id));
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            category.Id = _db.NextId();
            _db.Categories.Add(category);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            // Igual que la clave foránea Restrict de SQL Server.
            if (_db.Products.Any(p => p.CategoryId == category.Id))
                throw new InvalidOperationException("FK violation");
            _db.Categories.Remove(category);
        }
        return Task.CompletedTask;
    }

    public Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Products.Any(p => p.CategoryId == categoryId));
    }

    public Task<Dictionary<int, int>> GetProductCountsAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Products.GroupBy(p => p.CategoryId).ToDictionary(g => g.Key, g => g.Count()));
    }
}

public class FakeSupplierRepository : ISupplierRepository
{
    private readonly InMemoryDatabase _db;

    public FakeSupplierRepository(InMemoryDatabase db) => _db = db;

    public Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IReadOnlyList<Supplier> result = _db.Suppliers.OrderBy(s => s.Name).ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Suppliers.FirstOrDefault(s => s.Id == id));
    }

    public Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Suppliers.FirstOrDefault(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Supplier?> GetByIdentificationAsync(string identification, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Suppliers.FirstOrDefault(s => s.Identification == identification.Trim()));
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Suppliers.Any(s => s.Id == id));
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            supplier.Id = _db.NextId();
            _db.Suppliers.Add(supplier);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            if (_db.Products.Any(p => p.SupplierId == supplier.Id))
                throw new InvalidOperationException("FK violation");
            _db.Suppliers.Remove(supplier);
        }
        return Task.CompletedTask;
    }

    public Task<bool> HasProductsAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Products.Any(p => p.SupplierId == supplierId));
    }

    public Task<Dictionary<int, int>> GetProductCountsAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Products.GroupBy(p => p.SupplierId).ToDictionary(g => g.Key, g => g.Count()));
    }
}

public class FakeInventoryMovementRepository : IInventoryMovementRepository
{
    private readonly InMemoryDatabase _db;

    public FakeInventoryMovementRepository(InMemoryDatabase db) => _db = db;

    /// <summary>
    /// Permite simular que otra salida consumió las existencias justo antes de guardar.
    /// </summary>
    public bool SimulateConcurrentExit { get; set; }

    public Task<bool> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken = default)
    {
        // Mismo comportamiento que el repositorio real: stock y movimiento juntos, todo o nada.
        lock (_db.Sync)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == movement.ProductId);
            if (product is null)
                return Task.FromResult(false);

            if (movement.MovementType == MovementType.Exit)
            {
                if (SimulateConcurrentExit)
                    product.CurrentStock = 0;

                if (product.CurrentStock < movement.Quantity)
                    return Task.FromResult(false);

                product.CurrentStock -= movement.Quantity;
            }
            else
            {
                product.CurrentStock += movement.Quantity;
            }

            movement.StockAfter = product.CurrentStock;
            movement.Id = _db.NextId();
            _db.Movements.Add(movement);
            return Task.FromResult(true);
        }
    }

    public Task<int?> GetCurrentStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Products.FirstOrDefault(p => p.Id == productId)?.CurrentStock);
    }

    public Task<IReadOnlyList<InventoryMovement>> SearchAsync(
        int? productId = null,
        MovementType? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IEnumerable<InventoryMovement> query = _db.Movements;
            if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
            if (movementType.HasValue) query = query.Where(m => m.MovementType == movementType.Value);
            if (fromDate.HasValue) query = query.Where(m => m.Date >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(m => m.Date <= toDate.Value);

            IReadOnlyList<InventoryMovement> result = query
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Id)
                .Select(WithProduct)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<InventoryMovement>> GetAllAsync(CancellationToken cancellationToken = default)
        => SearchAsync(cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(int productId, CancellationToken cancellationToken = default)
        => SearchAsync(productId: productId, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InventoryMovement>> GetByTypeAsync(MovementType movementType, CancellationToken cancellationToken = default)
        => SearchAsync(movementType: movementType, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InventoryMovement>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => SearchAsync(fromDate: fromDate, toDate: toDate, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<InventoryMovement>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var all = await SearchAsync(cancellationToken: cancellationToken);
        return all.Take(count).ToList();
    }

    private InventoryMovement WithProduct(InventoryMovement movement)
    {
        movement.Product = _db.Products.FirstOrDefault(p => p.Id == movement.ProductId);
        return movement;
    }
}
