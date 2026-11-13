using ProjetDotNet.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjetDotNet;
using ProjetDotNet.Shared;




var builder = WebApplication.CreateBuilder(args.Where(a => a != "--demo" && a != "--provision-roles" && a != "--provision-admin").ToArray());
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
var demo = args.Contains("--demo");
if (demo && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("--demo is restricted to Development.");
var demoPath = Path.Combine(builder.Environment.ContentRootPath, ".artifacts", $"demo-{Guid.NewGuid():N}.db");
if (demo)
{
    Directory.CreateDirectory(Path.GetDirectoryName(demoPath)!);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".artifacts", "keys")));
}
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (demo) options.UseSqlite($"Data Source={demoPath}");
    else options.UseSqlServer(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity (fixed to match BookShoppingCartMvcUI)
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options => {
        // In development, don't require email confirmation for easier testing
        options.SignIn.RequireConfirmedAccount = false;
        // Password requirements
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

// ----------------------
// Missing DI services added
// ----------------------
builder.Services.AddTransient<IHomeRepository, HomeRepository>();
builder.Services.AddTransient<ICartRepository, CartRepository>();
builder.Services.AddTransient<IUserOrderRepository, UserOrderRepository>();
builder.Services.AddTransient<IStockRepository, StockRepository>();
builder.Services.AddTransient<ProjetDotNet.Repositories.ICategoryRepository, ProjetDotNet.Repositories.CategoryRepository>();
builder.Services.AddTransient<IFileService, FileService>();
builder.Services.AddTransient<IProductRepository, ProductRepository>();
builder.Services.AddTransient<IReportRepository, ReportRepository>();

var app = builder.Build();
if (demo)
{
    using var scope = app.Services.CreateScope();
    await DemoData.InitializeAsync(scope.ServiceProvider, builder.Configuration);
    app.Logger.LogInformation("Isolated demo database: {Path}", demoPath);
}
if (args.Contains("--provision-admin"))
{
    using var scope = app.Services.CreateScope();
    await AdminProvisioner.CreateAsync(scope.ServiceProvider, builder.Configuration);
    return;
}
if (args.Contains("--provision-roles"))
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedDefaultData(scope.ServiceProvider);
    return;
}

// Normal startup never migrates or seeds a database.
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Schema changes are applied explicitly with EF tooling.
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

public partial class Program { }
