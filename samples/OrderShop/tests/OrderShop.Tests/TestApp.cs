using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderShop.Api.Database;

namespace OrderShop.Tests;

// Starts the real app in memory: real endpoints, real EF Core, real SQLite.
// Only two things are swapped: the database file (for an in-memory one) and the currency API (for a fake).
public class TestApp : WebApplicationFactory<Program>
{
    public const decimal FakeRate = 1.10m;

    // Currency code the fake API refuses, to test what happens when the rate is unavailable.
    public const string UnknownCurrency = "XXX";

    // An in-memory SQLite database lives as long as its connection stays open.
    readonly SqliteConnection connection = new("Data Source=:memory:");

    public TestApp()
    {
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the file database registered in Program.cs, then add the in-memory one.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));

            services.AddHttpClient("CurrencyApi")
                .ConfigurePrimaryHttpMessageHandler(() => new FakeCurrencyApiHandler());
        });
    }

    public async Task Seed(params object[] entities)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    public async Task<T> Query<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }

    class FakeCurrencyApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.Query.Contains($"to={UnknownCurrency}"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var json = $$"""{ "rate": {{FakeRate.ToString(System.Globalization.CultureInfo.InvariantCulture)}} }""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}
