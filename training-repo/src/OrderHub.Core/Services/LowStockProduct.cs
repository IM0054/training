namespace OrderHub.Core.Services;

public sealed class LowStockProduct
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public int UnitsSoldLast30Days { get; init; }
}
