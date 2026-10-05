using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Customers;

namespace OrderShop.Tests.Features.Customers;

public class RegisterCustomerTests : IDisposable
{
    readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Registers_a_customer()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new RegisterCustomer.Request("Ann", " Ann@Shop.com "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterCustomer.Response>();
        Assert.Equal("Ann", body!.Name);
        Assert.Equal("ann@shop.com", body.Email);

        var saved = await app.Query(db => db.Customers.SingleAsync(c => c.Id == body.CustomerId));
        Assert.Equal("ann@shop.com", saved.Email);
    }

    [Fact]
    public async Task Returns_400_when_name_is_missing()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new RegisterCustomer.Request(" ", "ann@shop.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_when_email_is_missing()
    {
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new RegisterCustomer.Request("Ann", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_409_when_email_is_already_used()
    {
        await app.Seed(new Customer { Id = Guid.NewGuid(), Name = "Ann", Email = "ann@shop.com" });
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/customers", new RegisterCustomer.Request("Other Ann", "ANN@shop.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
