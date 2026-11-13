using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjetDotNet;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using ProjetDotNet.Models.DTOs;
using ProjetDotNet.Repositories;
using Xunit;

namespace Lustre.Tests;

public sealed class CommerceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _services;
    private readonly ApplicationDbContext _db;
    private readonly HttpContextAccessor _http = new();
    private readonly UserManager<IdentityUser> _users;

    public CommerceTests()
    {
        _connection.Open();
        var services = new ServiceCollection().AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_connection));
        services.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        _services = services.BuildServiceProvider();
        _db = _services.GetRequiredService<ApplicationDbContext>();
        _users = _services.GetRequiredService<UserManager<IdentityUser>>();
        _db.Database.EnsureCreated();
        _db.OrderStatuses.AddRange(new[] { "Pending", "Approved", "Shipped", "Delivered", "Cancelled" }
            .Select((name, i) => new OrderStatus { Id = i + 17, StatusId = i + 91, StatusName = name }));
        _db.Products.Add(new Product { Id = 1, ProductName = "Cleanser", BrandName = "Lustre", Price = 125.25m,
            Category = new Category { Id = 1, CategoryName = "Skincare" }, Stock = new Stock { Quantity = 5 } });
        _db.Products.Add(new Product { Id = 2, ProductName = "Serum", BrandName = "Lustre", Price = 80m,
            CategoryId = 1, Stock = new Stock { Quantity = 2 } });
        _db.SaveChanges();
        As("alice");
    }

    private void As(string name, string role = "User") => _http.HttpContext = new DefaultHttpContext
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, name),
            new Claim(ClaimTypes.Role, role) }, "tests"))
    };
    private CartRepository Cart => new(_db, _http, _users, NullLogger<CartRepository>.Instance);
    private UserOrderRepository Orders => new(_db, _users, _http);
    private static CheckoutModel Checkout() => new() { CheckoutToken = Guid.NewGuid(), Name = "Demo Customer",
        Email = "customer@example.test", Address = "Fictitious address", MobileNumber = "0600000000", PaymentMethod = "COD" };
    private async Task<Order> PlaceOrder()
    {
        await Cart.AddItem(1, 2);
        Assert.True(await Cart.DoCheckout(Checkout()));
        _db.ChangeTracker.Clear();
        return await _db.Orders.Include(o => o.OrderDetails).SingleAsync();
    }

    [Fact] public async Task CartAddsAndDecrementsOnlyOwnedItems()
    {
        await Cart.AddItem(1, 2);
        Assert.Equal(2, (await Cart.GetUserCart()).CartDetails.Single().Quantity);
        As("bob");
        Assert.Empty((await Cart.GetUserCart()).CartDetails);
        await Cart.RemoveItem(1);
        As("alice");
        await Cart.RemoveItem(1);
        Assert.Equal(1, (await Cart.GetUserCart()).CartDetails.Single().Quantity);
        await Cart.RemoveItem(1);
        Assert.Empty((await Cart.GetUserCart()).CartDetails);
    }
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(100)] [InlineData(6)]
    public async Task InvalidQuantitiesCannotModifyCart(int qty)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Cart.AddItem(1, qty));
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.CartDetails.ToListAsync());
    }
    [Fact] public async Task UnknownProductIsRejected() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => Cart.AddItem(999, 1));
    [Fact] public async Task UserCannotReadAnotherCart() =>
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Cart.GetCart("bob"));
    [Fact] public async Task UserCannotCountAnotherCart() =>
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Cart.GetCartItemCount("bob"));
    [Fact] public async Task EmptyCheckoutIsRejected() => Assert.False(await Cart.DoCheckout(Checkout()));
    [Fact] public async Task CheckoutUsesCurrentServerPriceAndConsumesCart()
    {
        await Cart.AddItem(1, 2);
        _db.Products.Local.Single(p => p.Id == 1).Price = 130.75m;
        await _db.SaveChangesAsync();
        Assert.True(await Cart.DoCheckout(Checkout()));
        _db.ChangeTracker.Clear();
        var detail = await _db.OrderDetails.SingleAsync();
        Assert.Equal(130.75m, detail.UnitPrice);
        Assert.Equal(261.50m, detail.UnitPrice * detail.Quantity);
        Assert.Equal(3, (await _db.Stocks.SingleAsync(s => s.ProductId == 1)).Quantity);
        Assert.Empty(await _db.CartDetails.ToListAsync());
        Assert.False((await _db.Orders.SingleAsync()).IsPaid);
    }
    [Fact] public async Task RepeatedCheckoutDoesNotDuplicateOrConsumeNewCart()
    {
        await Cart.AddItem(1, 1);
        var request = Checkout();
        Assert.True(await Cart.DoCheckout(request));
        await Cart.AddItem(2, 1);
        Assert.True(await Cart.DoCheckout(request));
        Assert.Single(await _db.Orders.ToListAsync());
        Assert.Single(await _db.CartDetails.ToListAsync());
    }
    [Fact] public async Task StockFailureRollsBackEarlierDeductionsAndOrder()
    {
        await Cart.AddItem(1, 1); await Cart.AddItem(2, 2);
        await _db.Stocks.Where(s => s.ProductId == 2).ExecuteUpdateAsync(u => u.SetProperty(s => s.Quantity, 0));
        Assert.False(await Cart.DoCheckout(Checkout()));
        Assert.Empty(await _db.Orders.ToListAsync());
        Assert.Empty(await _db.OrderDetails.ToListAsync());
        Assert.Equal(5, (await _db.Stocks.SingleAsync(s => s.ProductId == 1)).Quantity);
        Assert.Equal(2, await _db.CartDetails.CountAsync());
    }
    [Theory] [InlineData("Fake")] [InlineData("3")]
    public async Task InvalidPaymentMethodIsRejected(string method)
    {
        await Cart.AddItem(1, 1); var model = Checkout(); model.PaymentMethod = method;
        Assert.False(await Cart.DoCheckout(model)); Assert.Empty(await _db.Orders.ToListAsync());
    }
    [Fact] public async Task MissingIdempotencyTokenIsRejected()
    {
        var model = Checkout(); model.CheckoutToken = Guid.Empty;
        Assert.False(await Cart.DoCheckout(model));
    }
    [Fact] public async Task OrdersAreOwnerScopedAndAdminCanListAll()
    {
        var order = await PlaceOrder(); As("bob");
        Assert.Null(await Orders.GetOrderById(order.Id)); Assert.Empty(await Orders.UserOrders());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Orders.UserOrders(true));
        As("admin", "Admin"); Assert.Single(await Orders.UserOrders(true));
    }
    [Fact] public async Task StandardUserCannotMutateOrder()
    {
        var order = await PlaceOrder();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Orders.TogglePaymentStatus(order.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Orders.ChangeOrderStatus(new() { OrderId = order.Id, OrderStatusId = 18 }));
    }
    [Fact] public async Task CancellationRestoresStockExactlyOnce()
    {
        var order = await PlaceOrder(); As("admin", "Admin");
        var change = new UpdateOrderStatusModel { OrderId = order.Id, OrderStatusId = 21 };
        await Orders.ChangeOrderStatus(change); await Orders.ChangeOrderStatus(change);
        _db.ChangeTracker.Clear(); Assert.Equal(5, (await _db.Stocks.SingleAsync(s => s.ProductId == 1)).Quantity);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Orders.TogglePaymentStatus(order.Id));
    }
    [Fact] public async Task InvalidStatusJumpIsRejected()
    {
        var order = await PlaceOrder(); As("admin", "Admin");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Orders.ChangeOrderStatus(new() { OrderId = order.Id, OrderStatusId = 20 }));
    }
    [Fact] public async Task PaymentConfirmationIsIdempotentAndBlocksCancellation()
    {
        var order = await PlaceOrder(); As("admin", "Admin");
        await Orders.TogglePaymentStatus(order.Id); await Orders.TogglePaymentStatus(order.Id);
        Assert.True((await _db.Orders.SingleAsync()).IsPaid);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Orders.ChangeOrderStatus(new() { OrderId = order.Id, OrderStatusId = 21 }));
    }
    [Fact] public async Task ReferencedProductCannotBeDeleted()
    {
        await PlaceOrder(); var repo = new ProductRepository(_db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeleteProduct(_db.Products.Find(1)!));
    }
    [Theory] [InlineData(-1)] [InlineData(0)]
    public async Task InvalidProductPriceIsRejected(int price)
    {
        var repo = new ProductRepository(_db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddProduct(new Product { ProductName = "Test", BrandName = "Demo", CategoryId = 1, Price = price }));
    }
    [Fact] public async Task ProductCrudPreservesValidatedValues()
    {
        var repo = new ProductRepository(_db);
        var p = new Product { ProductName = "Mask", BrandName = "Demo", CategoryId = 1, Price = 10.50m };
        await repo.AddProduct(p); p.Price = 11.25m; await repo.UpdateProduct(p);
        Assert.Equal(11.25m, (await repo.GetProductById(p.Id))!.Price);
        await repo.DeleteProduct(p); Assert.Null(await repo.GetProductById(p.Id));
    }
    [Theory] [InlineData(-1, 1)] [InlineData(5, 999)]
    public async Task InvalidStockIsRejected(int qty, int id) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => new StockRepository(_db).ManageStock(new StockDTO { ProductId = id, Quantity = qty }));
    [Fact] public async Task ReportsExcludeUnpaidOrders()
    {
        await PlaceOrder(); var repo = new ReportRepository(_db);
        Assert.Empty(await repo.GetTopNSellingProductsByDate(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)));
        As("admin", "Admin"); await Orders.TogglePaymentStatus((await _db.Orders.SingleAsync()).Id);
        Assert.Equal(2, (await repo.GetTopNSellingProductsByDate(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1))).Single().TotalUnitSold);
    }
    [Fact] public async Task StaleStockFormCannotOverwriteSale()
    {
        var stock = new StockRepository(_db);
        await _db.Stocks.Where(s => s.ProductId == 1).ExecuteUpdateAsync(u => u.SetProperty(s => s.Quantity, 4));
        _db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.ManageStock(new StockDTO { ProductId = 1, Quantity = 12, OriginalQuantity = 5 }));
        Assert.Equal(4, (await _db.Stocks.SingleAsync(s => s.ProductId == 1)).Quantity);
    }
    [Fact] public async Task ConcurrentSqliteCheckoutsCannotOversellLastItem()
    {
        var file = Path.Combine(Path.GetTempPath(), "lustre-concurrency-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={file};Default Timeout=2;Pooling=False").Options;
        try
        {
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.OrderStatuses.Add(new OrderStatus { Id = 17, StatusName = "Pending", StatusId = 91 });
                setup.Products.Add(new Product { Id = 1, ProductName = "Last cleanser", BrandName = "Demo", Price = 15m,
                    Category = new Category { CategoryName = "Skincare" }, Stock = new Stock { Quantity = 1 } });
                foreach (var user in new[] { "first", "second" })
                    setup.ShoppingCarts.Add(new ShoppingCart { UserId = user, CartDetails = new List<CartDetail> {
                        new CartDetail { ProductId = 1, Quantity = 1, UnitPrice = 1m } } });
                await setup.SaveChangesAsync();
            }
            await using var first = new ApplicationDbContext(options);
            await using var second = new ApplicationDbContext(options);
            CartRepository Repository(ApplicationDbContext db, string user) => new(db,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user) }, "tests")) } },
                _users, NullLogger<CartRepository>.Instance);
            // HttpContextAccessor uses AsyncLocal; construct each accessor inside its own task.
            using var start = new ManualResetEventSlim();
            var one = Task.Run(async () => { var repo = Repository(first, "first"); start.Wait(); return await repo.DoCheckout(Checkout()); });
            var two = Task.Run(async () => { var repo = Repository(second, "second"); start.Wait(); return await repo.DoCheckout(Checkout()); });
            start.Set();
            var results = await Task.WhenAll(one, two);
            Assert.Equal(1, results.Count(success => success));
            await using var verify = new ApplicationDbContext(options);
            Assert.Equal(0, (await verify.Stocks.SingleAsync()).Quantity);
            Assert.Single(await verify.Orders.ToListAsync());
            Assert.Single(await verify.CartDetails.ToListAsync());
        }
        finally { if (File.Exists(file)) File.Delete(file); }
    }

    [Fact] public async Task ExplicitReferenceProvisioningPreservesStatusIdentifiersAndCreatesNoUsers()
    {
        var before = await _db.OrderStatuses.OrderBy(s => s.Id).Select(s => new { s.Id, s.StatusId }).ToListAsync();
        await DbSeeder.SeedDefaultData(_services);
        await DbSeeder.SeedDefaultData(_services);
        Assert.Equal(before, await _db.OrderStatuses.OrderBy(s => s.Id).Select(s => new { s.Id, s.StatusId }).ToListAsync());
        Assert.Empty(await _db.Users.ToListAsync());
        Assert.Equal(2, await _db.Roles.CountAsync());
    }
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(100)]
    public async Task TamperedCartQuantityCannotCheckout(int qty)
    {
        await Cart.AddItem(1, 1);
        await _db.CartDetails.ExecuteUpdateAsync(u => u.SetProperty(d => d.Quantity, qty));
        _db.ChangeTracker.Clear();
        Assert.False(await Cart.DoCheckout(Checkout()));
        Assert.Empty(await _db.Orders.ToListAsync());
        Assert.Equal(5, (await _db.Stocks.SingleAsync(s => s.ProductId == 1)).Quantity);
    }
    [Fact] public async Task OrderFollowsNamedLifecycleAndDeliveredIsTerminal()
    {
        var order = await PlaceOrder(); As("admin", "Admin");
        foreach (var status in new[] { 18, 19, 20 })
            await Orders.ChangeOrderStatus(new() { OrderId = order.Id, OrderStatusId = status });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Orders.ChangeOrderStatus(new() { OrderId = order.Id, OrderStatusId = 21 }));
        Assert.Equal(20, (await _db.Orders.SingleAsync()).OrderStatusId);
    }

    public void Dispose() { _services.Dispose(); _connection.Dispose(); }
}
