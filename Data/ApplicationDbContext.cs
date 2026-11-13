
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Models;

namespace ProjetDotNet.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<CartDetail>().Property(p => p.UnitPrice).HasPrecision(18, 2);
            builder.Entity<OrderDetail>().Property(p => p.UnitPrice).HasPrecision(18, 2);
            builder.Entity<ShoppingCart>().HasIndex(c => c.UserId).IsUnique();
            builder.Entity<CartDetail>().HasIndex(d => new { d.ShoppingCartId, d.ProductId }).IsUnique();
            builder.Entity<Order>().HasIndex(o => o.CheckoutToken).IsUnique().HasFilter("[CheckoutToken] IS NOT NULL");
            builder.Entity<Stock>().Property(s => s.Quantity).IsConcurrencyToken();
            builder.Entity<Order>().Property(o => o.OrderStatusId).IsConcurrencyToken();
            builder.Entity<Order>().Property(o => o.IsPaid).IsConcurrencyToken();
            builder.Entity<Product>().HasMany(p => p.OrderDetail).WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<Stock> Stocks { get; set; }


    }
}
