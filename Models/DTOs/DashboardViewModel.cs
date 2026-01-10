namespace ProjetDotNet.Models.DTOs;

public class DashboardViewModel
{
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockItems { get; set; }
}

