using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProjetDotNet.Repositories
{
    public class UserOrderRepository : IUserOrderRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public UserOrderRepository(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task ChangeOrderStatus(UpdateOrderStatusModel data)
        {
            RequireAdmin();
            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var order = await _db.Orders.Include(o => o.OrderStatus).Include(o => o.OrderDetails).SingleOrDefaultAsync(o => o.Id == data.OrderId);
            if (order == null)
                throw new InvalidOperationException($"Order with id {data.OrderId} not found");

            var next = await _db.OrderStatuses.FindAsync(data.OrderStatusId)
                ?? throw new InvalidOperationException("Unknown status.");
            var current = order.OrderStatus.StatusName;
            if (current == next.StatusName) return;
            var allowed = current switch
            {
                "Pending" => next.StatusName is "Approved" or "Cancelled",
                "Approved" => next.StatusName is "Shipped" or "Cancelled",
                "Shipped" => next.StatusName == "Delivered",
                _ => false
            };
            if (!allowed) throw new InvalidOperationException("Invalid order transition.");
            if (next.StatusName == "Cancelled")
            {
                if (order.IsPaid) throw new InvalidOperationException("Paid orders require a refund workflow before cancellation.");
                foreach (var item in order.OrderDetails.OrderBy(d => d.ProductId))
                {
                    var changed = await _db.Stocks.Where(s => s.ProductId == item.ProductId)
                        .ExecuteUpdateAsync(update => update.SetProperty(s => s.Quantity, s => s.Quantity + item.Quantity));
                    if (changed != 1) throw new InvalidOperationException("Stock record missing.");
                }
            }
            order.OrderStatusId = data.OrderStatusId;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task<Order?> GetOrderById(int id)
        {
            var userId = GetUserId();
            var admin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;
            return await _db.Orders.SingleOrDefaultAsync(o => o.Id == id && (admin || o.UserId == userId));
        }

        public async Task<IEnumerable<OrderStatus>> GetOrderStatuses()
        {
            return await _db.OrderStatuses.ToListAsync();
        }

        public async Task TogglePaymentStatus(int orderId)
        {
            RequireAdmin();
            var order = await _db.Orders.Include(o => o.OrderStatus).SingleOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                throw new InvalidOperationException($"Order with id {orderId} not found");

            if (order.OrderStatus.StatusName == "Cancelled") throw new InvalidOperationException("Cancelled orders cannot be paid.");
            // Simulated payment confirmation is idempotent; a retry must never undo payment.
            order.IsPaid = true;
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Order>> UserOrders(bool getAll = false)
        {
            if (getAll) RequireAdmin();
            var orders = _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .AsQueryable();

            if (!getAll)
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged in");

                orders = orders.Where(o => o.UserId == userId);
            }

            return await orders.ToListAsync();
        }

        private void RequireAdmin()
        {
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Admin") != true)
                throw new UnauthorizedAccessException();
        }

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
            return _userManager.GetUserId(principal) ?? throw new UnauthorizedAccessException();
        }
    }
}
