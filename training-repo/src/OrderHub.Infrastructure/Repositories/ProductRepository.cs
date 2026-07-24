using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold, DateTime soldSince) =>
        await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.StockQuantity < threshold)
            .Select(p => new LowStockProduct
            {
                Sku = p.Sku,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                UnitsSoldLast30Days = _db.OrderItems
                    .Where(i => i.ProductId == p.Id)
                    .Join(
                        _db.Orders.Where(o =>
                            o.CreatedAt >= soldSince &&
                            o.Status != OrderStatus.Cancelled),
                        item => item.OrderId,
                        order => order.Id,
                        (item, _) => item.Quantity)
                    .Sum(quantity => (int?)quantity) ?? 0
            })
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.Sku)
            .ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
