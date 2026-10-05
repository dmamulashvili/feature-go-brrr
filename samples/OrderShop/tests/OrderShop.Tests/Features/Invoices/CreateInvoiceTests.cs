using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Invoices;

namespace OrderShop.Tests.Features.Invoices;

public class CreateInvoiceTests : IDisposable
{
    readonly TestApp app = new();
    readonly Customer customer = new() { Id = Guid.NewGuid(), Name = "Ann", Email = "ann@shop.com" };

    public void Dispose() => app.Dispose();

    Order NewOrder(OrderStatus status) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customer.Id,
        CreatedAt = DateTime.UtcNow,
        Status = status,
        Total = 100.05m,
    };

    [Fact]
    public async Task Creates_an_invoice_in_the_requested_currency()
    {
        var order = NewOrder(OrderStatus.Placed);
        await app.Seed(customer, order);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/invoices", new CreateInvoice.Request(order.Id, "usd"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateInvoice.Response>();
        Assert.Equal("USD", body!.Currency);
        Assert.Equal(TestApp.FakeRate, body.ExchangeRate);
        // 100.05 * 1.10 = 110.055, rounded to cents away from zero.
        Assert.Equal(110.06m, body.Amount);

        var saved = await app.Query(db => db.Orders.SingleAsync(o => o.Id == order.Id));
        Assert.Equal(OrderStatus.Invoiced, saved.Status);
    }

    [Fact]
    public async Task Returns_400_when_currency_is_invalid()
    {
        var order = NewOrder(OrderStatus.Placed);
        await app.Seed(customer, order);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/invoices", new CreateInvoice.Request(order.Id, "dollars"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_404_when_order_does_not_exist()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/invoices", new CreateInvoice.Request(Guid.NewGuid(), "USD"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_409_when_order_is_cancelled()
    {
        var order = NewOrder(OrderStatus.Cancelled);
        await app.Seed(customer, order);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/invoices", new CreateInvoice.Request(order.Id, "USD"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Returns_409_when_order_is_already_invoiced()
    {
        var order = NewOrder(OrderStatus.Invoiced);
        await app.Seed(customer, order);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/invoices", new CreateInvoice.Request(order.Id, "USD"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Returns_502_when_exchange_rate_is_unavailable()
    {
        var order = NewOrder(OrderStatus.Placed);
        await app.Seed(customer, order);
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/invoices", new CreateInvoice.Request(order.Id, TestApp.UnknownCurrency));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(0, await app.Query(db => db.Invoices.CountAsync()));
    }
}
