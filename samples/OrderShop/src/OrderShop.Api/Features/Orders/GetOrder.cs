using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;

namespace OrderShop.Api.Features.Orders;

public static class GetOrder
{
    // ---- Contract ----
    public record Response(
        Guid OrderId,
        Guid CustomerId,
        string CustomerName,
        string Status,
        DateTime CreatedAt,
        decimal Total,
        List<Line> Lines);

    public record Line(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/orders/{id:guid}", Handle).WithTags("Orders");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var order = await SelectFromDatabase(id, db, ct);

        // Read-only feature: a single not-found check may stay in Handle.
        if (order is null) return Results.NotFound();

        return Results.Ok(order);
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<Response?> SelectFromDatabase(Guid id, AppDbContext db, CancellationToken ct)
    {
        return await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new Response(
                o.Id,
                o.CustomerId,
                o.Customer.Name,
                o.Status.ToString(),
                o.CreatedAt,
                o.Total,
                o.Lines
                    .Select(l => new Line(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(ct);
    }
}
