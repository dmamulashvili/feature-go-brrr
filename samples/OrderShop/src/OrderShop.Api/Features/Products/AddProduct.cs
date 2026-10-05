using OrderShop.Api.Database;
using OrderShop.Api.Database.Entities;

namespace OrderShop.Api.Features.Products;

public static class AddProduct
{
    // ---- Contract ----
    public record Request(string? Name, decimal Price, int Stock);
    public record Response(Guid ProductId, string Name, decimal Price, int Stock);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/products", Handle).WithTags("Products");

    // ---- Handle: the table of contents ----
    // No Select phase: this feature needs nothing from the database (R12).
    static async Task<IResult> Handle(Request req, AppDbContext db, CancellationToken ct)
    {
        var problem = LegitCheck(req);
        if (problem is not null) return problem;

        var product = ApplySauce(req);

        await YeetToDatabase(product, db, ct);

        return Results.Created($"/products/{product.Id}", new Response(product.Id, product.Name, product.Price, product.Stock));
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(Request req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest("Name is required.");

        if (req.Price <= 0)
            return Results.BadRequest("Price must be greater than zero.");

        if (req.Stock < 0)
            return Results.BadRequest("Stock cannot be negative.");

        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Product ApplySauce(Request req)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = req.Name!.Trim(),
            Price = req.Price,
            Stock = req.Stock,
        };
    }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(Product product, AppDbContext db, CancellationToken ct)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
    }
}
