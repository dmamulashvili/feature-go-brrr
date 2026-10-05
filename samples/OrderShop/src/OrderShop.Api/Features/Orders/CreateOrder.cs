using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;
using OrderShop.Api.Database.Entities;

namespace OrderShop.Api.Features.Orders;

public static class CreateOrder
{
    // ---- Contract ----
    public record Request(Guid CustomerId, List<Line>? Lines);
    public record Line(Guid ProductId, int Quantity);
    public record Response(Guid OrderId, decimal Total);

    record DatabaseData(bool CustomerExists, Dictionary<Guid, Product> Products);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/orders", Handle).WithTags("Orders");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Request req, AppDbContext db, CancellationToken ct)
    {
        var data = await SelectFromDatabase(req, db, ct);

        var problem = LegitCheck(req, data);
        if (problem is not null) return problem;

        var order = ApplySauce(req, data);

        await YeetToDatabase(order, db, ct);

        return Results.Created($"/orders/{order.Id}", new Response(order.Id, order.Total));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<DatabaseData> SelectFromDatabase(Request req, AppDbContext db, CancellationToken ct)
    {
        var customerExists = await db.Customers.AnyAsync(c => c.Id == req.CustomerId, ct);

        // Tracked (no AsNoTracking): ApplySauce lowers their stock and YeetToDatabase writes it.
        var productIds = req.Lines?.Select(l => l.ProductId).Distinct().ToList() ?? [];
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        return new DatabaseData(customerExists, products);
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(Request req, DatabaseData data)
    {
        if (req.Lines is null || req.Lines.Count == 0)
            return Results.BadRequest("An order needs at least one line.");

        if (req.Lines.Any(l => l.Quantity <= 0))
            return Results.BadRequest("Every quantity must be greater than zero.");

        if (!data.CustomerExists)
            return Results.NotFound("Customer not found.");

        var lineWithMissingProduct = req.Lines.FirstOrDefault(l => !data.Products.ContainsKey(l.ProductId));
        if (lineWithMissingProduct is not null)
            return Results.NotFound($"Product {lineWithMissingProduct.ProductId} not found.");

        // The same product may appear on several lines, so compare the summed quantity.
        foreach (var group in req.Lines.GroupBy(l => l.ProductId))
        {
            var product = data.Products[group.Key];
            if (group.Sum(l => l.Quantity) > product.Stock)
                return Results.Conflict($"Not enough stock for {product.Name}. In stock: {product.Stock}.");
        }

        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Order ApplySauce(Request req, DatabaseData data)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = req.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Placed,
        };

        foreach (var line in req.Lines!)
        {
            var product = data.Products[line.ProductId];
            product.Stock -= line.Quantity;

            order.Lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = line.Quantity,
            });
        }

        order.Total = order.Lines.Sum(l => l.UnitPrice * l.Quantity);
        return order;
    }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(Order order, AppDbContext db, CancellationToken ct)
    {
        // One SaveChanges writes the new order and the lowered stock in a single transaction.
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }
}
