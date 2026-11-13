using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProjetDotNet.Repositories;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _users;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<CartRepository> _logger;

    public CartRepository(ApplicationDbContext db, IHttpContextAccessor http,
        UserManager<IdentityUser> users, ILogger<CartRepository> logger)
        => (_db, _http, _users, _logger) = (db, http, users, logger);

    public async Task<int> AddItem(int productId, int qty)
    {
        if (productId <= 0 || qty < 1 || qty > 99)
            throw new InvalidOperationException("Choose a quantity between 1 and 99.");
        var userId = GetUserId();
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var product = await _db.Products.Include(p => p.Stock).SingleOrDefaultAsync(p => p.Id == productId)
            ?? throw new InvalidOperationException("Product not found.");
        var cart = await GetCart(userId);
        if (cart is null)
        {
            cart = new ShoppingCart { UserId = userId };
            _db.ShoppingCarts.Add(cart);
            await _db.SaveChangesAsync();
        }
        var item = await _db.CartDetails.SingleOrDefaultAsync(d => d.ShoppingCartId == cart.Id && d.ProductId == productId);
        var quantity = (item?.Quantity ?? 0) + qty;
        if (quantity > 99 || quantity > (product.Stock?.Quantity ?? 0))
            throw new InvalidOperationException("The requested quantity is unavailable.");
        if (item is null)
            _db.CartDetails.Add(new CartDetail { ShoppingCartId = cart.Id, ProductId = productId, Quantity = quantity, UnitPrice = product.Price });
        else
        {
            item.Quantity = quantity;
            item.UnitPrice = product.Price;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return await GetCartItemCount();
    }

    public async Task<int> RemoveItem(int productId)
    {
        var userId = GetUserId();
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.CartDetails.SingleOrDefaultAsync(d => d.ShoppingCart.UserId == userId && d.ProductId == productId);
        if (item is not null)
        {
            if (item.Quantity <= 1) _db.CartDetails.Remove(item);
            else item.Quantity--;
            await _db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return await GetCartItemCount();
    }

    public async Task<ShoppingCart> GetUserCart()
    {
        var userId = GetUserId();
        return (await _db.ShoppingCarts.Include(c => c.CartDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Stock)
            .Include(c => c.CartDetails).ThenInclude(d => d.Product).ThenInclude(p => p.Category)
            .SingleOrDefaultAsync(c => c.UserId == userId)) ?? new ShoppingCart { UserId = userId, CartDetails = new List<CartDetail>() };
    }

    public async Task<ShoppingCart> GetCart(string userId)
    {
        if (userId != GetUserId()) throw new UnauthorizedAccessException();
        return (await _db.ShoppingCarts.SingleOrDefaultAsync(c => c.UserId == userId))!;
    }

    public async Task<int> GetCartItemCount(string userId = "")
    {
        var current = GetUserId();
        if (!string.IsNullOrEmpty(userId) && userId != current) throw new UnauthorizedAccessException();
        return await _db.CartDetails.CountAsync(d => d.ShoppingCart.UserId == current);
    }

    public async Task<bool> DoCheckout(CheckoutModel model)
    {
        Validator.ValidateObject(model, new ValidationContext(model), true);
        if (model.CheckoutToken == Guid.Empty) return false;
        if (!Enum.TryParse<PaymentMethods>(model.PaymentMethod, out var payment) || !Enum.IsDefined(payment))
            return false;
        var userId = GetUserId();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try
        {
            tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            // A consumed cart cannot be ordered again. Serializable isolation protects the read/consume pair.
            if (await _db.Orders.AnyAsync(o => o.UserId == userId && o.CheckoutToken == model.CheckoutToken)) return true;
            var cart = await GetCart(userId);
            if (cart is null) return false;
            var items = await _db.CartDetails.Include(d => d.Product)
                .Where(d => d.ShoppingCartId == cart.Id).OrderBy(d => d.ProductId).ToListAsync();
            if (items.Count == 0) return false;
            var pending = await _db.OrderStatuses.SingleAsync(s => s.StatusName == "Pending");
            var order = new Order
            {
                CheckoutToken = model.CheckoutToken,
                UserId = userId,
                Name = model.Name,
                Email = model.Email,
                MobileNumber = model.MobileNumber,
                Address = model.Address,
                PaymentMethod = model.PaymentMethod,
                OrderStatusId = pending.Id,
                IsPaid = false,
                CreateDate = DateTime.UtcNow,
                OrderDetails = new()
            };
            foreach (var item in items)
            {
                if (item.Quantity is < 1 or > 99 || item.Product.Price <= 0)
                    throw new InvalidOperationException("Invalid cart item.");
                // Atomic conditional UPDATE: never read stock and blindly overwrite it.
                var changed = await _db.Stocks.Where(s => s.ProductId == item.ProductId && s.Quantity >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Quantity, s => s.Quantity - item.Quantity));
                if (changed != 1) throw new InvalidOperationException("Insufficient stock.");
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = decimal.Round(item.Product.Price, 2, MidpointRounding.AwayFromZero)
                });
            }
            _db.Orders.Add(order);
            _db.CartDetails.RemoveRange(items);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException or System.Data.Common.DbException)
        {
            if (tx is not null) await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            _logger.LogWarning(ex, "Checkout failed for the current user; transaction rolled back");
            return false;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    private string GetUserId() => _users.GetUserId(_http.HttpContext?.User
        ?? throw new UnauthorizedAccessException()) ?? throw new UnauthorizedAccessException();
}
