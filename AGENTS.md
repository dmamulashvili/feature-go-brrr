# AGENTS.md — Feature Go Brrr rules

This repository uses the **Feature Go Brrr** architecture. These rules are mandatory for anyone writing code here, human or AI agent. Follow them literally. When existing code disagrees with this file, this file wins. Cite rule numbers (R1–R17) when explaining a decision.

## Core idea

One feature = one static class = one file. The code reads top to bottom with zero hops to other files. Every feature runs the same named phases in the same order: **Select → Legit check → Apply the Sauce → Yeet**, a.k.a. **SLAY**.

## Hard rules

### Structure

- **R1.** One feature = one `public static class` = one file at `Features/{Area}/{Verb}{Noun}.cs`. Class name equals file name.
- **R2.** A feature MUST NOT reference another feature. Duplicate code instead.
- **R3.** Every feature MUST be registered by one explicit line in `AllFeatures.cs`. No assembly scanning, no reflection.
- **R4.** Shared code is limited to `Database/` (DbContext, entities, migrations) and infrastructure setup in `Program.cs`.

### Inside a feature

- **R5.** Sections appear in this order: Contract (records) → `Map` → `Handle` → phase methods.
- **R6.** `Handle` only calls phase methods, checks for a problem, and returns a result. No queries, no business rules.
- **R7.** Phase methods are `private static` and use only the names in the phase table below.
- **R8.** Phase methods MUST NOT call each other. Only `Handle` calls them.
- **R9.** Only `Handle` returns the final HTTP result. `LegitCheck` returns `IResult?`; `Handle` decides to return it.
- **R10.** All Select phases run first, then `LegitCheck`, then `ApplySauce`, then all Yeet phases. Nothing is written before checks pass.
- **R11.** `ApplySauce` does no I/O: no database, HTTP, file access, and no `await`.
- **R12.** Phases a feature does not need are left out, never written empty.

### Forbidden everywhere

- **R13.** No service classes, repository classes, or interfaces with a single implementation.
- **R14.** No MediatR, AutoMapper, FluentValidation, or any library that hides which code runs.
- **R15.** No base classes for features, no inheritance between features.

### Quality

- **R16.** Every feature has one matching test file: `tests/{Project}.Tests/Features/{Area}/{Verb}{Noun}Tests.cs`.
- **R17.** A feature file over ~300 lines is two features. Split it into two files, never into layers.

## Phase vocabulary

| Phase | Method name | Allowed | Not allowed | Returns |
| --- | --- | --- | --- | --- |
| Select | `SelectFromDatabase` | EF Core reads (`AsNoTracking` in read-only features) | Writes, decisions, HTTP results | A private data record or projection |
| Select | `SelectFrom{System}Api` | One external API read, e.g. `SelectFromCurrencyApi` | Writes, decisions | Parsed response |
| Select | `SelectFromFile` | Reading a file | Writes, decisions | File contents |
| Legit check | `LegitCheck` | Input, existence, permission and rule checks | Any I/O, changing data | `IResult?` — first problem, or `null` |
| Apply the Sauce | `ApplySauce` | Business rules (the secret sauce): calculations, creating/changing objects in memory | Any I/O | The new or changed entity |
| Yeet | `YeetToDatabase` | `Add`, `Remove`, `SaveChangesAsync` | Decisions | `Task` |
| Yeet | `YeetTo{System}` | One outbound call, e.g. `YeetToEmailService` | Decisions | `Task` |
| Yeet | `YeetToFile` | Writing a file | Decisions | `Task` |

Parameters: phases receive everything as parameters (request, data record, `db`, `IHttpClientFactory`, `CancellationToken`). No fields, no static state.

Yeet order: database first, then outside systems.

Read-only features: a single `if (x is null) return Results.NotFound();` may stay in `Handle`. Two or more checks go into `LegitCheck`.

## Feature skeleton

Copy `templates/Scroll.cs` or this:

```csharp
namespace MyApp.Api.Features.{Area};

public static class {Verb}{Noun}
{
    // ---- Contract ----
    public record Request(/* input */);
    public record Response(/* output */);

    record DatabaseData(/* everything loaded from the database */);

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/{route}", Handle).WithTags("{Area}");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Request req, AppDbContext db, CancellationToken ct)
    {
        var data = await SelectFromDatabase(req, db, ct);

        var problem = LegitCheck(req, data);
        if (problem is not null) return problem;

        var result = ApplySauce(req, data);

        await YeetToDatabase(result, db, ct);

        return Results.Ok(new Response(/* from result */));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<DatabaseData> SelectFromDatabase(Request req, AppDbContext db, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(Request req, DatabaseData data)
    {
        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Entity ApplySauce(Request req, DatabaseData data)
    {
        throw new NotImplementedException();
    }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(Entity result, AppDbContext db, CancellationToken ct)
    {
        db.Add(result);
        await db.SaveChangesAsync(ct);
    }
}
```

## Recipe: adding a feature

1. **Name it** `{Verb}{Noun}` in an area, e.g. `CancelOrder` in `Orders`. If the file exists, stop and ask.
2. **Copy** the closest existing feature (write or read-only) or `templates/Scroll.cs`. Rename class and namespace.
3. **Contract:** `Request` / `Response` with only the fields this feature needs.
4. **Route:** one `Map` call with verb, route and `WithTags("{Area}")`.
5. **Select phases:** load everything into one private data record.
6. **`LegitCheck`:** one `if` per rule with a clear message. Order: input checks → existence checks → business rules.
7. **`ApplySauce`:** in-memory only. Return the entity to save.
8. **Yeet phases:** database first, then outside systems.
9. **Delete unused phases** and their comment banners (R12).
10. **Register** one line in `AllFeatures.cs` under its area.
11. **Tests:** one happy path + one test per `return` in `LegitCheck`.
12. **Run** `dotnet build` and `dotnet test`. Both must pass, including architecture tests.
13. **Self-review** with the checklist below.

Missing entity or column: add it in `Database/Entities/` and create a migration. Entity changes are the only allowed edits outside the feature file, its test file and `AllFeatures.cs`.

## Forbidden patterns → write instead

| Do not write | Write instead | Rule |
| --- | --- | --- |
| `IOrderService` + `OrderService` | Logic inside the feature's phases | R13 |
| Repository wrapping `DbContext` | `db.Orders...` directly in Select/Yeet phases | R13 |
| MediatR request + handler | `Handle` inside the feature | R14 |
| AutoMapper profile | `new Response(...)` or a `Select` projection | R14 |
| Feature calling another feature | Copy the needed lines | R2 |
| `BaseFeature` / `FeatureBase<T>` | Copy the skeleton | R15 |
| `Helpers/` or `Common/` folder | Duplicate in each feature | R2, R4 |
| Validation attributes / validator classes | `if` checks in `LegitCheck` | R14 |
| `SaveChangesAsync` in `ApplySauce` | Move it to `YeetToDatabase` | R11 |
| Query inside `LegitCheck` | Load it in a Select phase first | R10 |
| Phase calling another phase | Call both from `Handle` | R8 |
| `throw` for expected failures (not found, conflict) | `return Results.NotFound(...)` from `LegitCheck` | R9 |
| Empty phases "for consistency" | Delete them | R12 |

**Single allowed exception:** when an identical pure calculation (e.g. tax) is needed in three or more features, it may become a `public static` method on a static class in `Database/Entities/` next to its entity. No I/O, no DI. Agents must ask a human before adding one.

## Review checklist

File and structure
- [ ] One new file at `Features/{Area}/{Verb}{Noun}.cs`, class name = file name (R1)
- [ ] No reference to another feature (R2)
- [ ] One new line in `AllFeatures.cs` (R3)
- [ ] No new service, repository, interface, base class or helper folder (R13, R15)

Inside the feature
- [ ] Order: Contract → `Map` → `Handle` → phases (R5)
- [ ] `Handle` only calls phases and returns results (R6)
- [ ] Phase names only from the phase table (R7)
- [ ] No phase calls another phase (R8)
- [ ] Select → Legit check → Apply the Sauce → Yeet, nothing written before checks (R10)
- [ ] `ApplySauce` has no `await` and no `db` parameter (R11)
- [ ] No empty or unused phases (R12)
- [ ] Under ~300 lines (R17)

Tests and build
- [ ] Matching test file exists (R16)
- [ ] Happy path + one test per check
- [ ] `dotnet build` and `dotnet test` pass

**Intern test:** can someone new explain what the feature does by reading only `Handle`? If not, fix the names.
