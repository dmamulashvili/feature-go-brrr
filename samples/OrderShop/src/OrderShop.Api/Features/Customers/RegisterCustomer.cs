using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;
using OrderShop.Api.Database.Entities;

namespace OrderShop.Api.Features.Customers;

public static class RegisterCustomer
{
    // ---- Contract ----
    public record Request(string? Name, string? Email);
    public record Response(Guid CustomerId, string Name, string Email);

    record DatabaseData(bool EmailAlreadyUsed);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/customers", Handle).WithTags("Customers");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Request req, AppDbContext db, CancellationToken ct)
    {
        var data = await SelectFromDatabase(req, db, ct);

        var problem = LegitCheck(req, data);
        if (problem is not null) return problem;

        var customer = ApplySauce(req);

        await YeetToDatabase(customer, db, ct);

        return Results.Created($"/customers/{customer.Id}", new Response(customer.Id, customer.Name, customer.Email));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<DatabaseData> SelectFromDatabase(Request req, AppDbContext db, CancellationToken ct)
    {
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";
        var emailAlreadyUsed = await db.Customers.AnyAsync(c => c.Email == email, ct);
        return new DatabaseData(emailAlreadyUsed);
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(Request req, DatabaseData data)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest("Name is required.");

        if (string.IsNullOrWhiteSpace(req.Email))
            return Results.BadRequest("Email is required.");

        if (data.EmailAlreadyUsed)
            return Results.Conflict("A customer with this email already exists.");

        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Customer ApplySauce(Request req)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = req.Name!.Trim(),
            // Stored lowercase so "Ann@Shop.com" and "ann@shop.com" count as the same email.
            Email = req.Email!.Trim().ToLowerInvariant(),
        };
    }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(Customer customer, AppDbContext db, CancellationToken ct)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
    }
}
