using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;
using OrderShop.Api.Database.Entities;

namespace OrderShop.Api.Features.Orders;

public static class CancelOrder
{
    // ---- Contract ----
    public record Response(Guid OrderId, string Status);

    record DatabaseData(Order? Order, Dictionary<Guid, Product> Products);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/orders/{id:guid}/cancel", Handle).WithTags("Orders");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var data = await SelectFromDatabase(id, db, ct);

        var problem = LegitCheck(data);
        if (problem is not null) return problem;

        var order = ApplySauce(data);

        await YeetToDatabase(db, ct);

        return Results.Ok(new Response(order.Id, order.Status.ToString()));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<DatabaseData> SelectFromDatabase(Guid id, AppDbContext db, CancellationToken ct)
    {
        // Tracked (no AsNoTracking): ApplySauce changes the order and the stock.
        var order = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        var productIds = order?.Lines.Select(l => l.ProductId).Distinct().ToList() ?? [];
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        return new DatabaseData(order, products);
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(DatabaseData data)
    {
        if (data.Order is null)
            return Results.NotFound("Order not found.");

        if (data.Order.Status == OrderStatus.Cancelled)
            return Results.Conflict("Order is already cancelled.");

        if (data.Order.Status == OrderStatus.Invoiced)
            return Results.Conflict("Order is invoiced and can no longer be cancelled.");

        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Order ApplySauce(DatabaseData data)
    {
        var order = data.Order!;
        order.Status = OrderStatus.Cancelled;

        // Put the reserved items back on the shelf.
        foreach (var line in order.Lines)
        {
            if (data.Products.TryGetValue(line.ProductId, out var product))
                product.Stock += line.Quantity;
        }

        return order;
    }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(AppDbContext db, CancellationToken ct)
    {
        // The order and products are tracked, so SaveChanges writes both in one transaction.
        await db.SaveChangesAsync(ct);
    }
}
