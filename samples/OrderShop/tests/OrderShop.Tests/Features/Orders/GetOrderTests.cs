using System.Net;
using System.Net.Http.Json;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Orders;

namespace OrderShop.Tests.Features.Orders;

public class GetOrderTests : IDisposable
{
    readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Returns_the_order_with_lines_and_customer_name()
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Ann", Email = "ann@shop.com" };
        var productId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Placed,
            Total = 100m,
            Lines = [new OrderLine { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Keyboard", UnitPrice = 50m, Quantity = 2 }],
        };
        await app.Seed(customer, order);
        var client = app.CreateClient();

        var response = await client.GetAsync($"/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetOrder.Response>();
        Assert.Equal("Ann", body!.CustomerName);
        Assert.Equal("Placed", body.Status);
        Assert.Equal(100m, body.Total);
        var line = Assert.Single(body.Lines);
        Assert.Equal(new GetOrder.Line(productId, "Keyboard", 50m, 2), line);
    }

    [Fact]
    public async Task Returns_404_when_order_does_not_exist()
    {
        var client = app.CreateClient();

        var response = await client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
