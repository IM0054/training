using System.ComponentModel.DataAnnotations;

namespace OrderHub.Web.ViewModels;

public class LowStockViewModel
{
    [Display(Name = "庫存門檻")]
    [Range(1, int.MaxValue, ErrorMessage = "庫存門檻必須大於 0")]
    public int Threshold { get; set; } = 10;

    public IReadOnlyList<LowStockProductRowViewModel> Products { get; set; } =
        Array.Empty<LowStockProductRowViewModel>();
}

public class LowStockProductRowViewModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int UnitsSoldLast30Days { get; set; }
}
