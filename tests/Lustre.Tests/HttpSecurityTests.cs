using System.Net;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjetDotNet.Data;
using Xunit;

namespace Lustre.Tests;

public sealed class TestHost : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public TestHost() => _connection.Open();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_connection));
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
                o.DefaultForbidScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, HeaderAuthentication>("Test", _ => { });
        });
    }
    public void CreateDatabase()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _connection.Dispose(); }
}

public sealed class HeaderAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user)) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()),
            new Claim(ClaimTypes.Role, Request.Headers["X-Test-Role"].ToString()) };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")), "Test")));
    }
}

public sealed class HttpSecurityTests : IDisposable
{
    private readonly TestHost _host = new();
    private readonly HttpClient _client;
    public HttpSecurityTests()
    {
        _client = _host.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        _host.CreateDatabase();
    }
    [Theory]
    [InlineData("/Cart/GetUserCart")] [InlineData("/UserOrder/UserOrders")]
    [InlineData("/AdminOperations/Dashboard")] [InlineData("/Product/Index")]
    public async Task AnonymousCannotAccessProtectedPages(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(url)).StatusCode);
    [Theory]
    [InlineData("/AdminOperations/Dashboard")] [InlineData("/Stock/Index")]
    [InlineData("/Reports/TopFiveSellingProducts")] [InlineData("/Product/Index")]
    public async Task CustomerCannotAccessAdminPages(string url)
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "alice");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(url)).StatusCode);
    }
    [Theory] [InlineData("/Cart/AddItem?productId=1")] [InlineData("/Cart/RemoveItem?productId=1")]
    [InlineData("/Product/DeleteProduct?id=1")] [InlineData("/AdminOperations/TogglePaymentStatus?orderId=1")]
    public async Task MutationsRejectGet(string url) => Assert.Equal(HttpStatusCode.MethodNotAllowed, (await _client.GetAsync(url)).StatusCode);
    [Theory] [InlineData("/Cart/AddItem?productId=1")] [InlineData("/Cart/Checkout")]
    [InlineData("/AdminOperations/UpdateOrderStatusAjax")] [InlineData("/Product/DeleteProduct?id=1")]
    public async Task MutationsRejectMissingCsrfToken(string url)
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync(url, new StringContent(""))).StatusCode);
    }
    [Fact] public async Task ValidCsrfHeaderReachesCartValidation()
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "alice");
        var page = await _client.GetStringAsync("/Cart/GetUserCart");
        var token = WebUtility.HtmlDecode(Regex.Match(page, "name=\"csrf-token\" content=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        _client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        var response = await _client.PostAsync("/Cart/AddItem?productId=999", new StringContent(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("unavailable", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task AdminCanReadDashboard()
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "admin"); _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/AdminOperations/Dashboard")).StatusCode);
    }
    public void Dispose() { _client.Dispose(); _host.Dispose(); }
}
