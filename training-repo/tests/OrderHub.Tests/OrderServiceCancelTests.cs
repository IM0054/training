using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderServiceCancelTests
{
    private static async Task<Order> CreateOrderWithStatusAsync(
        Core.Services.OrderService service,
        Infrastructure.Data.OrderHubDbContext db,
        OrderStatus status)
    {
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);
        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });
        var order = result.Value!;
        order.Status = status;
        await db.SaveChangesAsync();
        return order;
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public async Task CancelOrder_ActiveOrder_SetsStatusCancelled(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, db.Orders.Single(o => o.Id == order.Id).Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public async Task CancelOrder_ActiveOrder_RestoresProductStock(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);
        var item = order.Items.Single();
        var stockBeforeCancel = db.Products.Single(p => p.Id == item.ProductId).StockQuantity;

        var result = await service.CancelOrderAsync(order.Id);

        Assert.True(result.Success);
        db.ChangeTracker.Clear();
        Assert.Equal(
            stockBeforeCancel + item.Quantity,
            db.Products.Single(p => p.Id == item.ProductId).StockQuantity);
    }

    [Fact]
    public async Task CancelOrder_Twice_RestoresProductStockOnlyOnce()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, OrderStatus.Pending);
        var item = order.Items.Single();
        var stockBeforeCancel = db.Products.Single(p => p.Id == item.ProductId).StockQuantity;

        var firstResult = await service.CancelOrderAsync(order.Id);
        var secondResult = await service.CancelOrderAsync(order.Id);

        db.ChangeTracker.Clear();
        Assert.True(firstResult.Success);
        Assert.False(secondResult.Success);
        Assert.Equal(
            stockBeforeCancel + item.Quantity,
            db.Products.Single(p => p.Id == item.ProductId).StockQuantity);
    }

    [Fact]
    public async Task CancelOrder_MultipleItems_RestoresEveryProduct()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var firstProduct = TestSetup.AddProduct(db, stock: 10);
        var secondProduct = TestSetup.AddProduct(db, stock: 20);
        var createResult = await service.CreateOrderAsync(customer.Id, new[]
        {
            new NewOrderLine(firstProduct.Id, 2),
            new NewOrderLine(secondProduct.Id, 3)
        });

        var cancelResult = await service.CancelOrderAsync(createResult.Value!.Id);

        db.ChangeTracker.Clear();
        Assert.True(cancelResult.Success);
        Assert.Equal(10, db.Products.Single(p => p.Id == firstProduct.Id).StockQuantity);
        Assert.Equal(20, db.Products.Single(p => p.Id == secondProduct.Id).StockQuantity);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CancelOrder_NotCancellableStatus_Fails(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.False(result.Success);
        Assert.Equal(initialStatus, db.Orders.Single(o => o.Id == order.Id).Status);
    }

    [Fact]
    public async Task CancelOrder_NotFound_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var result = await service.CancelOrderAsync(12345);

        Assert.False(result.Success);
        Assert.Contains("找不到", result.ErrorMessage);
    }
}
