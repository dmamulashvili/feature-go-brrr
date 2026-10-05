using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;

namespace OrderShop.Api.Features.Products;

public static class ListProducts
{
    // ---- Contract ----
    public record Response(List<Item> Products);
    public record Item(Guid ProductId, string Name, decimal Price, int Stock);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/products", Handle).WithTags("Products");

    // ---- Handle: the table of contents ----
    // GET /products?inStockOnly=true
    static async Task<IResult> Handle(bool? inStockOnly, AppDbContext db, CancellationToken ct)
    {
        var products = await SelectFromDatabase(inStockOnly ?? false, db, ct);

        return Results.Ok(new Response(products));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<List<Item>> SelectFromDatabase(bool inStockOnly, AppDbContext db, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();

        if (inStockOnly)
            query = query.Where(p => p.Stock > 0);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new Item(p.Id, p.Name, p.Price, p.Stock))
            .ToListAsync(ct);
    }
}
