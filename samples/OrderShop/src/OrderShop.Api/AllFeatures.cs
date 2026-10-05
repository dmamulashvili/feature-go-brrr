using OrderShop.Api.Features.Customers;
using OrderShop.Api.Features.Invoices;
using OrderShop.Api.Features.Orders;
using OrderShop.Api.Features.Products;

namespace OrderShop.Api;

// Every endpoint in the app, one line each. No scanning, no reflection (R3).
public static class AllFeatures
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Customers
        RegisterCustomer.Map(app);
        GetCustomer.Map(app);

        // Products
        AddProduct.Map(app);
        ListProducts.Map(app);

        // Orders
        CreateOrder.Map(app);
        GetOrder.Map(app);
        CancelOrder.Map(app);

        // Invoices
        CreateInvoice.Map(app);
    }
}
