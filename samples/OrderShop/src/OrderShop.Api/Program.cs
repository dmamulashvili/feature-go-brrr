using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using OrderShop.Api;
using OrderShop.Api.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    // Every feature has its own nested "Request" and "Response", so prefix schema names with the feature name.
    options.CreateSchemaReferenceId = type => type.Type.DeclaringType is { } feature
        ? feature.Name + type.Type.Name
        : OpenApiOptions.CreateDefaultSchemaReferenceId(type);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient("CurrencyApi", client =>
    client.BaseAddress = new Uri(builder.Configuration["CurrencyApi:BaseUrl"]!));

var app = builder.Build();

// Apply migrations on startup so the sample runs with no setup steps.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.MapOpenApi();
AllFeatures.Map(app);

app.Run();

// Lets the test project start the app with WebApplicationFactory<Program>.
public partial class Program;
