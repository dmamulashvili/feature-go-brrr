# 🚨 Feature Go Brrr 📻

### The Architecture Your Architect Doesn't Want You to Know

> CRUD found dead. SLAY is the main suspect 💅<br>
> AI read the rules… then actually FOLLOWED them 🤖<br>
> SOLID just turned liquid 💧<br>
> The Big Boi era has begun 📻

**One class. One feature. Zero hops.**

**Feature Go Brrr** is a .NET Web API architecture your intern can debug and your AI agent can follow.

Every feature is **one static class in one file**. The code reads **top to bottom**. No services, no repositories, no interfaces, no mediators, no mapping libraries. Put one breakpoint at the top, press F10, and you've seen everything.

```text
Code start top.
Code go down.
Code end.
Good.

Code not jump to other file.
Code not hide in interface.
Me F10. Me understand.
```

## Speak Brrr (a quick glossary)

Before we get serious, learn the language:

| Term | Meaning |
| --- | --- |
| **The Scroll** | One feature file. "Where's the logic?" "In The Scroll." |
| **Big Boi** | A feature class. "This should be its own Big Boi" = split it into a new feature. |
| **Hop** | Jumping to another file to follow the code. We allow zero. |
| **Scroll-Driven Development** | Writing features that are understood by scrolling down. |
| **Top Down Town** | The `Features/` folder. Population: your features. |
| **SLAY** 💅 | **S**elect → **L**egit check → **A**pply the Sauce 🧂 → **Y**eet. The four phases, in order. CRUD is dead. Every feature must SLAY. |
| **Feature Go Brrr** | What you say when all tests pass. 🚀 |
| **SOLID is liquid now** | The answer to "where are the interfaces?" 💧 |

---

## Why

Most .NET codebases spread one feature across 6–10 files: controller, service interface, service, repository interface, repository, mapper profile, validator, DTOs. To understand "create an order" you press F12, land on an interface, search for the implementation, and repeat.

That costs everyone:

- **Interns** get lost in indirection before they understand the business.
- **AI agents** need to read and edit many files per change, and drift from the conventions.
- **Reviewers** review sprawling diffs for small features.

Feature Go Brrr puts locality first: **everything about a feature lives in one place**, in a fixed, predictable shape.

## Vibe code it. Scroll debug it.

Let your AI agent write the features. Seriously. The rules in [AGENTS.md](AGENTS.md) are strict and boring on purpose, so agents follow them and every feature comes out the same shape.

Then, one day, it happens:

```text
03:12  Production is on fire 🔥
03:13  You open the feature that broke
03:13  It's one file
03:14  You scroll down
03:15  You found it
03:16  You go back to sleep
```

You don't need to know what the agent was thinking. Neither did the agent. 🤷

You don't need to find the implementation of the interface of the factory of the service, written by someone who left in 2019. 🕵️

**It's all in The Scroll.** Select, Legit check, Apply, Yeet. Read it top to bottom, like the agent wrote it. Because it did. And for once, it followed the rules.

Vibe code responsibly: the agent writes it, the architecture keeps it readable, and you fix it at 3 AM with one eye open and zero "Go To Implementation" clicks. Your architect calls it "not enterprise-ready". Your pager calls it "resolved". 📟

## The whole idea in one file (yes, one)

```csharp
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
    static async Task<DatabaseData> SelectFromDatabase(Request req, AppDbContext db, CancellationToken ct) { ... }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(Request req, DatabaseData data) { ... }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Order ApplySauce(Request req, DatabaseData data) { ... }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(Order order, AppDbContext db, CancellationToken ct) { ... }
}
```

`Handle` reads like a sentence. Each phase does one kind of work, so **the kind of bug tells you which method to open**. Wrong data? Bad Select. Let through something it shouldn't? Failed the Legit check. Wrong total? Check the sauce. Not saved? Never got yeeted.

## The four phases: SLAY 💅

CRUD is dead. Every feature must **SLAY**: Select the data, Legit check it, Apply the Sauce, Yeet it. 🧂

| Phase | Method names | Does | Never does |
| --- | --- | --- | --- |
| **S**elect | `SelectFromDatabase`, `SelectFrom{System}Api`, `SelectFromFile` | Reads everything it needs, up front | Decide or write. It's a reader, not a leader. |
| **L**egit check | `LegitCheck` | Returns the first red flag, or `null` | Any I/O. No database calls mid legit check. |
| **A**pply the Sauce 🧂 | `ApplySauce` | Calculates and changes objects in memory | Any I/O. Works in RAM only. |
| **Y**eet | `YeetToDatabase`, `YeetTo{System}`, `YeetToFile` | Writes data | Decide. Too late for opinions. |

Order is always **Select → Legit check → Apply the Sauce → Yeet**. Nothing is written until the legit check passes. Phases a feature doesn't need are left out, not written empty "for consistency".

## The rules (short version)

1. One feature = one static class = one file at `Features/{Area}/{Verb}{Noun}.cs`. One Big Boi per Scroll.
2. Features never reference other features. Duplicate instead. Yes, really. Copy-paste is a feature, not a bug.
3. Every feature is registered with one explicit line in `AllFeatures.cs`. No assembly scanning, no reflection, no magic. Not on the list? Doesn't exist.
4. `Handle` only calls phases and returns results. It's the table of contents, not the whole book.
5. Phase methods are `private static`, use the standard names, and never call each other. No group chats between phases.
6. No service classes, repositories, single-implementation interfaces, MediatR, AutoMapper or feature base classes. `IOrderService` has been let go. We wish it well in its future endeavors.
7. Every feature has one matching test file that calls the real endpoint. Real HTTP, real database, real confidence.

The full, strict rule set (R1–R17), the recipe for adding a feature, and the review checklist are in **[AGENTS.md](AGENTS.md)**. It is written to be followed literally by AI coding agents and newcomers alike.

## Project structure

```text
src/MyApp.Api/
├── Program.cs              # infrastructure setup only
├── AllFeatures.cs          # every endpoint, one line each
├── Features/
│   ├── Orders/
│   │   ├── CreateOrder.cs
│   │   ├── GetOrder.cs
│   │   └── CancelOrder.cs
│   └── Customers/
│       └── RegisterCustomer.cs
└── Database/
    ├── AppDbContext.cs
    ├── Entities/
    └── Migrations/
tests/MyApp.Tests/
└── Features/Orders/CreateOrderTests.cs   # one test file per feature
```

## Quick start

1. Look at the sample: [`samples/OrderShop`](samples/OrderShop).
2. Copy [`templates/Scroll.cs`](templates/Scroll.cs) to `Features/{Area}/{Verb}{Noun}.cs`.
3. Fill in the phases, delete the ones you don't need.
4. Add one line to `AllFeatures.cs`.
5. Add a test file. Run `dotnet test`. Feature go brrr.

## Trade-offs (a.k.a. things your architect will yell about)

**"But you're duplicating code!"**
Yes. Two features that load the same customer both write `db.Customers.FirstOrDefaultAsync(...)`. That's one line. We've seen people build a 4-file abstraction to avoid typing one line. We call that "DRY", as in the codebase is now a desert. 🏜️

**"How do you unit test the business logic?"**
We test the whole feature through HTTP against a real database, one test file per feature. If you really miss mocking, you can mock your architect's feelings instead. 🫠

**"What about huge, complex domains?"**
If your domain has 400 shared invariants and a whiteboard that looks like a crime scene, Feature Go Brrr might not be your friend. For everything else, the rules allow one small, pure, shared calculation when three or more features need the exact same result. We're flat, not reckless.

**"This isn't SOLID!"**
SOLID is liquid now. 💧

**TL;DR:** if your team values "anyone can open any file and understand it" over "look how many layers I made", welcome to Top Down Town.

## License

MIT