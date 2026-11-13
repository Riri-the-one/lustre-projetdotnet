using ProjetDotNet.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetDotNet.Data;
using ProjetDotNet.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ProjetDotNet.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class AdminOperationsController : Controller
{
    private readonly IUserOrderRepository _userOrderRepository;
    private readonly ApplicationDbContext _context;

    public AdminOperationsController(IUserOrderRepository userOrderRepository, ApplicationDbContext context)
    {
        _userOrderRepository = userOrderRepository;
        _context = context;
    }

    public async Task<IActionResult> AllOrders()
    {
        var orders = await _userOrderRepository.UserOrders(true);
        var statuses = await _userOrderRepository.GetOrderStatuses();
        ViewBag.OrderStatuses = statuses.Select(s => new { Id = s.Id, Name = s.StatusName });
        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> TogglePaymentStatus(int orderId)
    {
        try
        {
            await _userOrderRepository.TogglePaymentStatus(orderId);
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AdminOperationsController>>().LogError(ex, "Payment confirmation failed");
            TempData["errorMessage"] = "Payment could not be confirmed.";
        }
        return RedirectToAction(nameof(AllOrders));
    }

    public async Task<IActionResult> UpdateOrderStatus(int orderId)
    {
        var order = await _userOrderRepository.GetOrderById(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with id:{orderId} does not found.");
        }
        var orderStatusList = (await _userOrderRepository.GetOrderStatuses()).Select(orderStatus =>
        {
            return new SelectListItem { Value = orderStatus.Id.ToString(), Text = orderStatus.StatusName, Selected = order.OrderStatusId == orderStatus.Id };
        });
        var data = new UpdateOrderStatusModel
        {
            OrderId = orderId,
            OrderStatusId = order.OrderStatusId,
            OrderStatusList = orderStatusList
        };
        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel data)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                data.OrderStatusList = (await _userOrderRepository.GetOrderStatuses()).Select(orderStatus =>
                {
                    return new SelectListItem { Value = orderStatus.Id.ToString(), Text = orderStatus.StatusName, Selected = orderStatus.Id == data.OrderStatusId };
                });

                return View(data);
            }
            await _userOrderRepository.ChangeOrderStatus(data);
            TempData["msg"] = "Updated successfully";
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AdminOperationsController>>().LogError(ex, "Status update failed");
            TempData["msg"] = "Something went wrong";
        }
        return RedirectToAction(nameof(UpdateOrderStatus), new { orderId = data.OrderId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatusAjax([FromBody] UpdateOrderStatusModel data)
    {
        try
        {
            if (!ModelState.IsValid || data.OrderId <= 0 || data.OrderStatusId <= 0)
            {
                return Json(new { success = false, message = "Invalid data" });
            }
            await _userOrderRepository.ChangeOrderStatus(data);
            var newStatus = await _context.OrderStatuses.FindAsync(data.OrderStatusId);
            return Json(new {
                success = true,
                message = "Status updated successfully",
                newStatus = newStatus?.StatusName ?? "Unknown",
                newStatusId = data.OrderStatusId
            });
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AdminOperationsController>>().LogError(ex, "Status update failed");
            return Json(new { success = false, message = "The order could not be updated. Refresh and check its current status." });
        }
    }


    public async Task<IActionResult> Dashboard()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderDetails)
            .ToListAsync();
        var productCount = await _context.Products.CountAsync();
        var categoryCount = await _context.Categories.CountAsync();
        var lowStockCount = await _context.Products.CountAsync(p => p.Stock == null || p.Stock.Quantity < 10);

        var totalRevenue = orders
            .Where(o => o.IsPaid && o.OrderStatus.StatusName != "Cancelled" && o.OrderDetails != null)
            .Sum(o => o.OrderDetails.Sum(od => od.Quantity * (decimal)od.UnitPrice));
        var pendingOrders = orders.Count(o => o.OrderStatus.StatusName == "Pending");
        var lowStockItems = lowStockCount;

        var viewModel = new DashboardViewModel
        {
            TotalOrders = orders.Count,
            TotalProducts = productCount,
            TotalCategories = categoryCount,
            TotalRevenue = totalRevenue,
            PendingOrders = pendingOrders,
            LowStockItems = lowStockItems
        };

        return View(viewModel);
    }

}
