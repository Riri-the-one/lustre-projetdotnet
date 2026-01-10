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

    public async Task<IActionResult> TogglePaymentStatus(int orderId)
    {
        try
        {
            await _userOrderRepository.TogglePaymentStatus(orderId);
        }
        catch (Exception)
        {
            // log exception here
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
        catch (Exception)
        {
            // catch exception here
            TempData["msg"] = "Something went wrong";
        }
        return RedirectToAction(nameof(UpdateOrderStatus), new { orderId = data.OrderId });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateOrderStatusAjax([FromBody] UpdateOrderStatusModel data)
    {
        try
        {
            if (!ModelState.IsValid || data.OrderId <= 0 || data.OrderStatusId <= 0)
            {
                return Json(new { success = false, message = "Invalid data" });
            }
            await _userOrderRepository.ChangeOrderStatus(data);
            var updatedOrder = await _context.Orders
                .Include(o => o.OrderStatus)
                .FirstOrDefaultAsync(o => o.Id == data.OrderId);
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
            return Json(new { success = false, message = "Something went wrong: " + ex.Message });
        }
    }


    public async Task<IActionResult> Dashboard()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ToListAsync();
        var products = await _context.Products.ToListAsync();
        var categories = await _context.Categories.ToListAsync();
        var stocks = await _context.Stocks.ToListAsync();
        
        var totalRevenue = orders
            .Where(o => o.IsPaid && o.OrderDetails != null)
            .Sum(o => o.OrderDetails.Sum(od => od.Quantity * (decimal)od.UnitPrice));
        var pendingOrders = orders.Count(o => !o.IsPaid);
        var lowStockItems = stocks.Count(s => s.Quantity < 10);
        
        var viewModel = new DashboardViewModel
        {
            TotalOrders = orders.Count,
            TotalProducts = products.Count,
            TotalCategories = categories.Count,
            TotalRevenue = totalRevenue,
            PendingOrders = pendingOrders,
            LowStockItems = lowStockItems
        };
        
        return View(viewModel);
    }

}
