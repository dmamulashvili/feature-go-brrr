using System.Net;
using System.Net.Http.Json;
using OrderShop.Api.Database.Entities;
using OrderShop.Api.Features.Customers;

namespace OrderShop.Tests.Features.Customers;

public class GetCustomerTests : IDisposable
{
    readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Returns_the_customer()
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Ann", Email = "ann@shop.com" };
        await app.Seed(customer);
        var client = app.CreateClient();

        var response = await client.GetAsync($"/customers/{customer.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCustomer.Response>();
        Assert.Equal(new GetCustomer.Response(customer.Id, "Ann", "ann@shop.com", 0), body);
    }

    [Fact]
    public async Task Returns_404_when_customer_does_not_exist()
    {
        var client = app.CreateClient();

        var response = await client.GetAsync($"/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
