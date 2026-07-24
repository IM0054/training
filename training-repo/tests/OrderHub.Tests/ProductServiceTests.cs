using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndSortsByStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 9, sku: "SKU-STOCK09");
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-STOCK02");
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-STOCK10");

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Equal(
            new[] { "SKU-STOCK02", "SKU-STOCK09" },
            result.Value!.Select(p => p.Sku));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 1, isActive: false, sku: "SKU-INACTIVE");
        TestSetup.AddProduct(db, stock: 2, isActive: true, sku: "SKU-ACTIVE");

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal("SKU-ACTIVE", result.Value![0].Sku);
    }

    [Fact]
    public async Task GetLowStock_RecentSalesExcludeCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2, sku: "SKU-SALES");

        db.Orders.AddRange(
            CreateOrder(customer.Id, product.Id, OrderStatus.Shipped, DateTime.UtcNow.AddDays(-1), 3),
            CreateOrder(customer.Id, product.Id, OrderStatus.Cancelled, DateTime.UtcNow.AddDays(-1), 5),
            CreateOrder(customer.Id, product.Id, OrderStatus.Shipped, DateTime.UtcNow.AddDays(-31), 7));
        db.SaveChanges();

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Equal(3, result.Value!.Single().UnitsSoldLast30Days);
    }

    [Fact]
    public async Task GetLowStock_NonPositiveThresholdFails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        var result = await service.GetLowStockAsync(0);

        Assert.False(result.Success);
        Assert.Contains("大於 0", result.ErrorMessage);
    }

    private static Order CreateOrder(
        int customerId,
        int productId,
        OrderStatus status,
        DateTime createdAt,
        int quantity) =>
        new()
        {
            CustomerId = customerId,
            Status = status,
            CreatedAt = createdAt,
            Items =
            {
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPriceSnapshot = 100m
                }
            }
        };
}
