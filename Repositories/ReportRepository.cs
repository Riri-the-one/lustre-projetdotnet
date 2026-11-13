using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ProjetDotNet.Repositories;

[Authorize(Roles = nameof(Roles.Admin))]
public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;
    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TopNSoldProductModel>> GetTopNSellingProductsByDate(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate) throw new ArgumentException("Start date must precede end date.");
        return await _context.OrderDetails.AsNoTracking()
            .Where(d => d.Order.IsPaid && d.Order.OrderStatus.StatusName != "Cancelled"
                && d.Order.CreateDate >= startDate && d.Order.CreateDate <= endDate)
            .GroupBy(d => new { d.ProductId, d.Product.ProductName, d.Product.BrandName })
            .Select(g => new { g.Key.ProductName, g.Key.BrandName, TotalUnitSold = g.Sum(d => d.Quantity) })
            .OrderByDescending(p => p.TotalUnitSold).Take(5)
            .Select(p => new TopNSoldProductModel(p.ProductName!, p.BrandName!, p.TotalUnitSold)).ToListAsync();
    }
}

public interface IReportRepository
{
    Task<IEnumerable<TopNSoldProductModel>> GetTopNSellingProductsByDate(DateTime startDate, DateTime endDate);
}
