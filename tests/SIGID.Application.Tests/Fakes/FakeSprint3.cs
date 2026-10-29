using SIGID.Application.DTOs.Sales;
using SIGID.Application.Interfaces.Persistence;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Security;
using SIGID.Domain.Entities;

namespace SIGID.Application.Tests.Fakes;

public class FakeAdministratorRepository : IAdministratorRepository
{
    private readonly InMemoryDatabase _db;

    public FakeAdministratorRepository(InMemoryDatabase db) => _db = db;

    public Task<Administrator?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Administrators.FirstOrDefault(a => a.Id == id));
    }

    public Task<Administrator?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Administrators.FirstOrDefault(a => string.Equals(a.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Administrator?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.Administrators.FirstOrDefault(a => string.Equals(a.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Administrator?> GetByUserNameOrEmailAsync(string userNameOrEmail, CancellationToken cancellationToken = default)
    {
        var value = userNameOrEmail.Trim();
        lock (_db.Sync)
            return Task.FromResult(_db.Administrators.FirstOrDefault(a =>
                string.Equals(a.UserName, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Email, value, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Administrators.Count > 0);
    }

    public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync) return Task.FromResult(_db.Administrators.Any(a => a.Id == id && a.IsActive));
    }

    public Task AddAsync(Administrator administrator, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            // Numeración propia (como una tabla aparte) para no alterar los Id del resto de datos.
            administrator.Id = _db.Administrators.Count + 1;
            _db.Administrators.Add(administrator);
        }
        return Task.CompletedTask;
    }
}

public class FakeSaleRepository : ISaleRepository
{
    private readonly InMemoryDatabase _db;

    public FakeSaleRepository(InMemoryDatabase db) => _db = db;

    public Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            // Igual que el índice único de SQL Server.
            if (_db.Sales.Any(s => s.RequestId == sale.RequestId))
                throw new SIGID.Application.Exceptions.BusinessException("No se pudo registrar la venta.");

            sale.Id = _db.NextId();
            foreach (var detail in sale.Details)
            {
                detail.Id = _db.NextId();
                detail.SaleId = sale.Id;
            }
            _db.Sales.Add(sale);
        }
        return Task.CompletedTask;
    }

    public Task<Sale?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            var sale = _db.Sales.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(sale is null ? null : Copy(sale));
        }
    }

    public Task<Sale?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            var sale = _db.Sales.FirstOrDefault(s => s.RequestId == requestId);
            return Task.FromResult(sale is null ? null : Copy(sale));
        }
    }

    public Task<IReadOnlyList<SaleDetail>> GetDetailsAsync(int saleId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            var sale = _db.Sales.FirstOrDefault(s => s.Id == saleId);
            IReadOnlyList<SaleDetail> result = sale is null ? new List<SaleDetail>() : Copy(sale).Details.ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<Sale>> SearchAsync(int? productId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IEnumerable<Sale> query = _db.Sales;
            if (productId.HasValue) query = query.Where(s => s.Details.Any(d => d.ProductId == productId.Value));
            if (fromDate.HasValue) query = query.Where(s => s.Date >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(s => s.Date <= toDate.Value);

            IReadOnlyList<Sale> result = query
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.Id)
                .Select(Copy)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken cancellationToken = default)
        => SearchAsync(cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => SearchAsync(fromDate: fromDate, toDate: toDate, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Sale>> GetByProductAsync(int productId, CancellationToken cancellationToken = default)
        => SearchAsync(productId: productId, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<ProductSalesByDayDto>> GetSoldQuantitiesByDayAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IReadOnlyList<ProductSalesByDayDto> result = _db.Sales
                .Where(s => s.Date >= fromDate && s.Date <= toDate)
                .SelectMany(s => s.Details.Select(d => new { s.Date, d.ProductId, d.Quantity }))
                .GroupBy(x => new { x.ProductId, Day = x.Date.Date })
                .Select(g =>
                {
                    var product = _db.Products.First(p => p.Id == g.Key.ProductId);
                    return new ProductSalesByDayDto
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = product.Name,
                        SKU = product.SKU,
                        Day = g.Key.Day,
                        Quantity = g.Sum(x => x.Quantity)
                    };
                })
                .OrderBy(r => r.Day)
                .ThenBy(r => r.ProductName)
                .ToList();
            return Task.FromResult(result);
        }
    }

    // Devuelve una copia, como hace AsNoTracking: modificar el resultado no cambia lo guardado.
    private Sale Copy(Sale sale) => new()
    {
        Id = sale.Id,
        Date = sale.Date,
        RequestId = sale.RequestId,
        Details = sale.Details.Select(d => new SaleDetail
        {
            Id = d.Id,
            SaleId = d.SaleId,
            ProductId = d.ProductId,
            Quantity = d.Quantity,
            Product = _db.Products.FirstOrDefault(p => p.Id == d.ProductId)
        }).ToList()
    };
}

/// <summary>
/// Simula una transacción: si la operación falla, restaura existencias, movimientos y ventas.
/// </summary>
public class FakeTransactionRunner : ITransactionRunner
{
    private readonly InMemoryDatabase _db;

    public FakeTransactionRunner(InMemoryDatabase db) => _db = db;

    public int Commits { get; private set; }

    public int Rollbacks { get; private set; }

    private Dictionary<int, int> _stocks = new();

    /// <summary>
    /// Simula un cambio hecho y confirmado por OTRA operación: no se deshace con el rollback.
    /// </summary>
    public void ApplyExternalChange(Product product, int newStock)
    {
        product.CurrentStock = newStock;
        _stocks[product.Id] = newStock;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await _db.TransactionLock.WaitAsync(cancellationToken);
        try
        {
            return await RunAsync(operation);
        }
        finally
        {
            _db.TransactionLock.Release();
        }
    }

    private async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        Dictionary<int, int> stocks;
        int movementCount, saleCount;
        lock (_db.Sync)
        {
            stocks = _stocks = _db.Products.ToDictionary(p => p.Id, p => p.CurrentStock);
            movementCount = _db.Movements.Count;
            saleCount = _db.Sales.Count;
        }

        try
        {
            var result = await operation();
            Commits++;
            return result;
        }
        catch
        {
            lock (_db.Sync)
            {
                foreach (var product in _db.Products)
                    if (stocks.TryGetValue(product.Id, out var stock)) product.CurrentStock = stock;
                _db.Movements.RemoveRange(movementCount, _db.Movements.Count - movementCount);
                _db.Sales.RemoveRange(saleCount, _db.Sales.Count - saleCount);
            }
            Rollbacks++;
            throw;
        }
    }
}

/// <summary>
/// Hash de prueba (no seguro): solo para comprobar que el servicio no guarda la contraseña tal cual.
/// La aplicación usa PasswordHashingService (PasswordHasher de ASP.NET Core).
/// </summary>
public class FakePasswordHasher : IPasswordHashingService
{
    public int Verifications { get; private set; }

    public string HashPassword(string password) => "HASH::" + new string(password.Reverse().ToArray());

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        Verifications++;
        return passwordHash == HashPassword(providedPassword);
    }
}
