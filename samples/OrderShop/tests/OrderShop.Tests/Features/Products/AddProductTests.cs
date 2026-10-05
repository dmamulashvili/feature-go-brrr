using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Features.Products;

namespace OrderShop.Tests.Features.Products;

public class AddProductTests : IDisposable
{
    readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Adds_a_product()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/products", new AddProduct.Request("Keyboard", 49.90m, 10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AddProduct.Response>();
        var saved = await app.Query(db => db.Products.SingleAsync(p => p.Id == body!.ProductId));
        Assert.Equal("Keyboard", saved.Name);
        Assert.Equal(49.90m, saved.Price);
        Assert.Equal(10, saved.Stock);
    }

    [Fact]
    public async Task Returns_400_when_name_is_missing()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/products", new AddProduct.Request("", 49.90m, 10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_when_price_is_not_positive()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/products", new AddProduct.Request("Keyboard", 0m, 10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_when_stock_is_negative()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/products", new AddProduct.Request("Keyboard", 49.90m, -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
