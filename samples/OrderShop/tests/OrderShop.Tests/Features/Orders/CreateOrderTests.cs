using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Orders;

namespace OrderShop.Tests.Features.Orders;

public class CreateOrderTests : IDisposable
{
    readonly TestApp app = new();
    readonly Customer customer = new() { Id = Guid.NewGuid(), Name = "Ann", Email = "ann@shop.com" };
    readonly Product keyboard = new() { Id = Guid.NewGuid(), Name = "Keyboard", Price = 50m, Stock = 5 };
    readonly Product mouse = new() { Id = Guid.NewGuid(), Name = "Mouse", Price = 20m, Stock = 1 };

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Creates_an_order_lowers_stock_and_calculates_total()
    {
        await app.Seed(customer, keyboard, mouse);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrder.Request(customer.Id,
        [
            new CreateOrder.Line(keyboard.Id, 2),
            new CreateOrder.Line(mouse.Id, 1),
        ]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateOrder.Response>();
        Assert.Equal(120m, body!.Total);

        var stock = await app.Query(db => db.Products.ToDictionaryAsync(p => p.Id, p => p.Stock));
        Assert.Equal(3, stock[keyboard.Id]);
        Assert.Equal(0, stock[mouse.Id]);

        var order = await app.Query(db => db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == body.OrderId));
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public async Task Returns_400_when_there_are_no_lines()
    {
        await app.Seed(customer);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrder.Request(customer.Id, []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_when_a_quantity_is_not_positive()
    {
        await app.Seed(customer, keyboard);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrder.Request(customer.Id,
            [new CreateOrder.Line(keyboard.Id, 0)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_404_when_customer_does_not_exist()
    {
        await app.Seed(keyboard);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrder.Request(Guid.NewGuid(),
            [new CreateOrder.Line(keyboard.Id, 1)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_404_when_a_product_does_not_exist()
    {
        await app.Seed(customer);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrder.Request(customer.Id,
            [new CreateOrder.Line(Guid.NewGuid(), 1)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_409_when_stock_is_too_low_and_changes_nothing()
    {
        await app.Seed(customer, mouse);
        var client = app.CreateClient();

        // Two lines of 1 each: fine alone, but together they need 2 and only 1 is in stock.
        var response = await client.PostAsJsonAsync("/orders", new CreateOrder.Request(customer.Id,
        [
            new CreateOrder.Line(mouse.Id, 1),
            new CreateOrder.Line(mouse.Id, 1),
        ]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await app.Query(db => db.Products.Where(p => p.Id == mouse.Id).Select(p => p.Stock).SingleAsync()));
        Assert.Equal(0, await app.Query(db => db.Orders.CountAsync()));
    }
}
