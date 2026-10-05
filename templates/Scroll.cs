// The Scroll: copy this file to Features/{Area}/{Verb}{Noun}.cs, then:
//   1. Rename the namespace, the class and the route.
//   2. Replace "Entity" with your entity type.
//   3. Fill in the phases. Delete the ones you don't need, banners included (R12).
//   4. Register it with one line in AllFeatures.cs (R3).
//   5. Add Features/{Area}/{Verb}{Noun}Tests.cs (R16).

using Microsoft.EntityFrameworkCore;
using MyApp.Api.Database;
using MyApp.Api.Database.Entities;

namespace MyApp.Api.Features.Area;

public static class VerbNoun
{
    // ---- Contract ----
    // Only the fields this feature needs.
    public record Request(Guid Id);
    public record Response(Guid Id);

    // Everything the Select phases load, in one place.
    record DatabaseData(Entity? Entity);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/route", Handle).WithTags("Area");

    // ---- Handle: the table of contents ----
    // Only calls phases and returns the result (R6). Someone new should understand the feature from here alone.
    static async Task<IResult> Handle(Request req, AppDbContext db, CancellationToken ct)
    {
        var data = await SelectFromDatabase(req, db, ct);

        var problem = LegitCheck(req, data);
        if (problem is not null) return problem;

        var result = ApplySauce(req, data);

        await YeetToDatabase(result, db, ct);

        return Results.Ok(new Response(result.Id));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    // Use AsNoTracking() in read-only features. Leave tracking on when Apply changes what you load.
    static async Task<DatabaseData> SelectFromDatabase(Request req, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.Set<Entity>().FirstOrDefaultAsync(e => e.Id == req.Id, ct);
        return new DatabaseData(entity);
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    // Order: input checks, then existence checks, then business rules. No I/O.
    static IResult? LegitCheck(Request req, DatabaseData data)
    {
        if (req.Id == Guid.Empty)
            return Results.BadRequest("Id is required.");

        if (data.Entity is null)
            return Results.NotFound("Entity not found.");

        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    // No await, no db parameter (R11).
    static Entity ApplySauce(Request req, DatabaseData data)
    {
        var entity = data.Entity!;
        // Change the entity here.
        return entity;
    }

    // ---- 4. YEET: writes only ----
    // Database first, then outside systems (YeetTo{System}, YeetToFile).
    static async Task YeetToDatabase(Entity result, AppDbContext db, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
