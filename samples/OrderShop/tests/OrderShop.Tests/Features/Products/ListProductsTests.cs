using System.Net;
using System.Net.Http.Json;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Products;

namespace OrderShop.Tests.Features.Products;

public class ListProductsTests : IDisposable
{
    readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    async Task SeedProducts() => await app.Seed(
        new Product { Id = Guid.NewGuid(), Name = "Mouse", Price = 19.90m, Stock = 0 },
        new Product { Id = Guid.NewGuid(), Name = "Keyboard", Price = 49.90m, Stock = 5 });

    [Fact]
    public async Task Lists_all_products_sorted_by_name()
    {
        await SeedProducts();
        var client = app.CreateClient();

        var response = await client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListProducts.Response>();
        Assert.Equal(new[] { "Keyboard", "Mouse" }, body!.Products.Select(p => p.Name));
    }

    [Fact]
    public async Task Lists_only_products_in_stock_when_asked()
    {
        await SeedProducts();
        var client = app.CreateClient();

        var response = await client.GetAsync("/products?inStockOnly=true");

        var body = await response.Content.ReadFromJsonAsync<ListProducts.Response>();
        Assert.Equal(new[] { "Keyboard" }, body!.Products.Select(p => p.Name));
    }
}
