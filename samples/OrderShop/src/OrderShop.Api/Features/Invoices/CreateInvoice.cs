using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database;
using OrderShop.Api.Database.Entities;

namespace OrderShop.Api.Features.Invoices;

public static class CreateInvoice
{
    // ---- Contract ----
    public record Request(Guid OrderId, string? Currency);
    public record Response(Guid InvoiceId, Guid OrderId, string Currency, decimal ExchangeRate, decimal Amount);

    record DatabaseData(Order? Order);
    record CurrencyApiResponse(decimal Rate);

    // Order totals are stored in this currency.
    const string BaseCurrency = "EUR";

    // ---- Route ----
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/invoices", Handle).WithTags("Invoices");

    // ---- Handle: the table of contents ----
    static async Task<IResult> Handle(Request req, AppDbContext db, IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var data = await SelectFromDatabase(req, db, ct);
        var rate = await SelectFromCurrencyApi(req, httpClientFactory, ct);

        var problem = LegitCheck(req, data, rate);
        if (problem is not null) return problem;

        var invoice = ApplySauce(req, data, rate!.Value);

        await YeetToDatabase(invoice, db, ct);

        return Results.Created(
            $"/invoices/{invoice.Id}",
            new Response(invoice.Id, invoice.OrderId, invoice.Currency, invoice.ExchangeRate, invoice.Amount));
    }

    // ---- 1. SELECT: reads only, no decisions ----
    static async Task<DatabaseData> SelectFromDatabase(Request req, AppDbContext db, CancellationToken ct)
    {
        // Tracked (no AsNoTracking): ApplySauce marks the order as invoiced.
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        return new DatabaseData(order);
    }

    // Returns null when the rate is unavailable; LegitCheck turns that into a 502.
    static async Task<decimal?> SelectFromCurrencyApi(Request req, IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("CurrencyApi");
        var to = Uri.EscapeDataString(req.Currency?.Trim().ToUpperInvariant() ?? "");

        try
        {
            using var response = await client.GetAsync($"rates?from={BaseCurrency}&to={to}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadFromJsonAsync<CurrencyApiResponse>(ct);
            return body?.Rate;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // ---- 2. LEGIT CHECK: returns the first problem, or null when allowed ----
    static IResult? LegitCheck(Request req, DatabaseData data, decimal? rate)
    {
        if (string.IsNullOrWhiteSpace(req.Currency) || req.Currency.Trim().Length != 3)
            return Results.BadRequest("Currency must be a 3-letter code, e.g. USD.");

        if (data.Order is null)
            return Results.NotFound("Order not found.");

        if (data.Order.Status == OrderStatus.Cancelled)
            return Results.Conflict("A cancelled order cannot be invoiced.");

        if (data.Order.Status == OrderStatus.Invoiced)
            return Results.Conflict("Order is already invoiced.");

        if (rate is null or <= 0m)
            return Results.Problem("Exchange rate is unavailable. Try again later.", statusCode: StatusCodes.Status502BadGateway);

        return null;
    }

    // ---- 3. APPLY: in-memory changes only, no I/O ----
    static Invoice ApplySauce(Request req, DatabaseData data, decimal rate)
    {
        var order = data.Order!;
        order.Status = OrderStatus.Invoiced;

        return new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CreatedAt = DateTime.UtcNow,
            Currency = req.Currency!.Trim().ToUpperInvariant(),
            ExchangeRate = rate,
            // Money is rounded to cents once, here, so every invoice rounds the same way.
            Amount = Math.Round(order.Total * rate, 2, MidpointRounding.AwayFromZero),
        };
    }

    // ---- 4. YEET: writes only ----
    static async Task YeetToDatabase(Invoice invoice, AppDbContext db, CancellationToken ct)
    {
        // Adds the invoice and saves the tracked order status in one transaction.
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
    }
}
