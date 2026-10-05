# FAQ

## What about reuse?

Features don't reuse each other (R2). If two features need the same query or check, each one writes it. That is deliberate: shared code couples features, so a change made for one can break the other.

What you *can* share:

- `Database/`: the `DbContext`, entities and migrations (R4).
- Infrastructure in `Program.cs`: database setup, HTTP clients, authentication, OpenAPI.
- One narrow exception: when an identical **pure** calculation (no I/O, no DI) is needed in three or more features, it may become a `public static` method next to its entity in `Database/Entities/`. Ask a human first.

If you notice the same five lines in many features, that's often fine. If you notice the same fifty lines, the features may be one feature, or the domain may need a different architecture (see [why.md](why.md)).

## How do I unit test business logic?

Every feature has one test file that calls the real endpoint through `WebApplicationFactory` with an in-memory SQLite database (see `samples/OrderShop/tests`). That covers routing, binding, the checks, the rules and the database mapping in one go, and the tests don't break when you refactor inside the feature.

The recipe: one happy path plus one test per `return` in `LegitCheck`.

Because `LegitCheck` and `ApplySauce` do no I/O (R11), they are easy to test directly if a calculation has many edge cases. They are `private`, so you'd test them through the endpoint with different inputs. If that becomes painful, it's a sign the calculation is large enough for its own endpoint or for the pure-calculation exception above.

## What about large domains?

Split large features into more features, never into layers (R17). "Create order" and "apply discount code" can be two endpoints and two files.

If the domain has many invariants that must hold across many operations, Feature Go Brrr will force you to repeat them, and repeated invariants drift. That's the point where a rich domain model (aggregates enforcing their own rules) earns its keep. Be honest about which kind of domain you have.

## How do transactions across features work?

They don't, because a feature never calls another feature. One HTTP request runs one feature, and one feature saves in one `SaveChangesAsync` call, which EF Core wraps in a transaction. In the sample, `CreateOrder` writes the new order and the lowered stock in one save; `CancelOrder` writes the status and the restored stock in one save.

If a use case needs changes that two features would make, it is a new feature that does all of it. Copy the lines you need.

For changes across systems (database plus email, payment provider, message bus), Yeet phases run database first, then outside systems. If the outside call can fail after the database commit, use an outbox table: the feature saves an outbox row in the same `SaveChangesAsync`, and a background job sends it.

## What about background jobs?

A background job is a feature without an HTTP route. Put it in `Features/{Area}/{Verb}{Noun}.cs` with the same phases. Instead of `Map`, register the hosted service or scheduler entry in `AllFeatures.cs` or `Program.cs`, and have it call `Handle` for each run. `Handle` returns a result or a status instead of `IResult`; the rest of the rules apply unchanged.

Keep the job's loop and scheduling (infrastructure) separate from the work it does (the feature). Test it by calling the job's entry point against the test database.

## How does authorization work?

Two levels:

- **Who may call the endpoint at all:** standard ASP.NET Core authorization. Configure authentication and policies in `Program.cs`, and add `.RequireAuthorization("PolicyName")` in the feature's `Map`. Everyone sees it on the route line.
- **Whether this user may do this to this data** ("only the customer who owns the order may cancel it"): that's a rule like any other. Load what you need in the Select phase, pass the current user (`ClaimsPrincipal`) to `LegitCheck`, and return `Results.Forbid()` or `Results.NotFound()` from there. Add a test for it like any other check.

No authorization service, no handler per rule unless ASP.NET Core requires it for a policy.
