using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Orders;

namespace OrderShop.Tests.Features.Orders;

public class CancelOrderTests : IDisposable
{
    readonly TestApp app = new();
    readonly Customer customer = new() { Id = Guid.NewGuid(), Name = "Ann", Email = "ann@shop.com" };
    readonly Product keyboard = new() { Id = Guid.NewGuid(), Name = "Keyboard", Price = 50m, Stock = 3 };

    public void Dispose() => app.Dispose();

    Order NewOrder(OrderStatus status) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customer.Id,
        CreatedAt = DateTime.UtcNow,
        Status = status,
        Total = 100m,
        Lines = [new OrderLine { Id = Guid.NewGuid(), ProductId = keyboard.Id, ProductName = "Keyboard", UnitPrice = 50m, Quantity = 2 }],
    };

    [Fact]
    public async Task Cancels_the_order_and_restores_stock()
    {
        var order = NewOrder(OrderStatus.Placed);
        await app.Seed(customer, keyboard, order);
        var client = app.CreateClient();

        var response = await client.PostAsync($"/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CancelOrder.Response>();
        Assert.Equal("Cancelled", body!.Status);
        Assert.Equal(5, await app.Query(db => db.Products.Where(p => p.Id == keyboard.Id).Select(p => p.Stock).SingleAsync()));
    }

    [Fact]
    public async Task Returns_404_when_order_does_not_exist()
    {
        var client = app.CreateClient();

        var response = await client.PostAsync($"/orders/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_409_when_order_is_already_cancelled()
    {
        var order = NewOrder(OrderStatus.Cancelled);
        await app.Seed(customer, keyboard, order);
        var client = app.CreateClient();

        var response = await client.PostAsync($"/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(3, await app.Query(db => db.Products.Where(p => p.Id == keyboard.Id).Select(p => p.Stock).SingleAsync()));
    }

    [Fact]
    public async Task Returns_409_when_order_is_invoiced()
    {
        var order = NewOrder(OrderStatus.Invoiced);
        await app.Seed(customer, keyboard, order);
        var client = app.CreateClient();

        var response = await client.PostAsync($"/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
