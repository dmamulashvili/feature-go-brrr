# Why Feature Go Brrr

This page makes the case for the architecture, compares it with layered and Clean Architecture, and is honest about what it costs.

## The problem it solves

In a typical layered .NET API, "create an order" is spread over many files:

```text
OrdersController.cs
IOrderService.cs
OrderService.cs
IOrderRepository.cs
OrderRepository.cs
CreateOrderRequestValidator.cs
OrderMappingProfile.cs
CreateOrderRequest.cs / OrderDto.cs
```

Each file is small and clean. The feature as a whole is not. To answer "what happens when someone creates an order?" you follow calls through interfaces, find implementations, and keep the whole chain in your head. The structure is organised around technical concerns (controllers, services, data access), but changes almost always arrive per feature.

Feature Go Brrr flips this. The unit of organisation is the feature, and a feature is one file:

- **Locality.** Everything that runs for one endpoint is in one place. Reading it is scrolling, not navigating.
- **Predictable shape.** Every feature runs Select → Legit check → Apply → Yeet with the same method names. Once you've read one feature, you know where to look in all of them.
- **Bug type tells you the method.** Wrong data loaded? `SelectFromDatabase`. Request accepted that should be rejected? `LegitCheck`. Wrong total? `ApplySauce`. Not persisted? `YeetToDatabase`.
- **Small, isolated diffs.** Adding a feature touches one new file, one line in `AllFeatures.cs` and one test file. Changing a feature cannot break another feature, because features don't reference each other.
- **Easy for newcomers and coding agents.** Strict, boring rules are easy to follow and easy to check. The architecture tests enforce the important ones.

## Compared with layered architecture

| | Layered (Controller / Service / Repository) | Feature Go Brrr |
| --- | --- | --- |
| Files per feature | 5 to 10 | 1 (+ 1 test file) |
| Where business logic lives | Services, sometimes spread across several | `ApplySauce` and `LegitCheck` of that feature |
| Reuse | Shared services and repositories | Duplication, plus one narrow exception for pure calculations |
| Risk of a change | A shared service change can affect many endpoints | A change affects one endpoint |
| Test style | Often unit tests with mocks per layer | Integration tests per feature through HTTP |

Layered code optimises for reuse between features. In practice much of that "reuse" is a repository method used once, or a service method that grows flags (`includeLines`, `forInvoice`) as more callers share it.

## Compared with Clean Architecture

Clean Architecture protects the domain from infrastructure through dependency inversion: the domain defines interfaces, infrastructure implements them. That pays off when you really do swap infrastructure, or when a rich domain model has invariants that must hold no matter who calls it.

Most web APIs don't swap their database, and most of their rules are checks and calculations for one use case. For those apps, Clean Architecture brings projects, interfaces and mapping layers whose main job is to point at each other. Feature Go Brrr keeps the useful parts:

- Business rules are still isolated: `LegitCheck` and `ApplySauce` do no I/O (R11), so they are as pure as a domain service.
- I/O is still at the edges: Select phases first, Yeet phases last (R10).

And it drops the parts that cost more than they return for this kind of app: interfaces with one implementation, repositories over EF Core (which already is a repository and unit of work), and mapping libraries.

## Compared with Vertical Slice Architecture

Feature Go Brrr is a strict flavour of vertical slices. Vertical slice architecture says "organise by feature" but leaves the inside of each slice open, so teams often reintroduce MediatR handlers, pipeline behaviours and shared services. Feature Go Brrr fixes the inside too: fixed phase names, fixed order, no hidden dispatch (R14).

## The trade-offs

These are real. Choose the architecture knowing them.

- **Duplication.** Two features that load the same customer both write the query. Rules that apply to several features are written in each. When a rule changes, you have to find every copy. Mitigation: features are small and `grep` is fast; the architecture tests make sure copies don't turn into hidden dependencies.
- **No central domain model.** Invariants live in the features that enforce them, not in the entity. If your domain has many invariants that must hold across many operations (finance, insurance, scheduling), a rich domain model may serve you better.
- **Integration-heavy testing.** Tests go through HTTP and a real database. They are slower than pure unit tests and need a working database setup (SQLite in memory here).
- **Large features.** A feature with many rules grows. R17 says split at ~300 lines into two features, never into layers. Some workflows don't split cleanly.
- **Unfamiliar to some teams.** Developers trained in layered architecture may see it as "no architecture". The rules and the tests are what make it one.

## When to use it

Good fit:

- CRUD-plus APIs, back-offices, internal tools, line-of-business apps.
- Teams with newcomers, frequent rotation, or heavy use of coding agents.
- Codebases where "what does this endpoint do?" is the most common question.

Poor fit:

- Domains dominated by shared invariants and complex aggregates.
- Libraries and SDKs, where reuse is the product.
- Systems where infrastructure really must be swappable behind abstractions.
