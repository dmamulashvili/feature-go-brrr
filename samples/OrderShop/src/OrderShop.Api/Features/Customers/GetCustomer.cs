using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;

namespace OrderShop.Api.Features.Customers;

public static class GetCustomer
{
    // ---- Contract ----
    public record Response(Guid CustomerId, string Name, string Email, int OrderCount);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/customers/{id:guid}", Handle).WithTags("Customers");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var customer = await SelectFromDatabase(id, db, ct);

        // Read-only feature: a single not-found check may stay in Handle.
        if (customer is null) return Results.NotFound();

        return Results.Ok(customer);
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<Response?> SelectFromDatabase(Guid id, AppDbContext db, CancellationToken ct)
    {
        return await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new Response(
                c.Id,
                c.Name,
                c.Email,
                db.Orders.Count(o => o.CustomerId == c.Id)))
            .FirstOrDefaultAsync(ct);
    }
}
