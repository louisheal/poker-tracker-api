using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.HandHistories;
using PokerTrackerApi.PreflopSpots;
using PokerTrackerApi.HandImport;
using PokerTrackerApi.HandImport.Parsers;
using PokerTrackerApi.HandImport.Readers;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IHandHistoryRepository, HandHistoryRepository>();
builder.Services.AddScoped<IHandImportRepository, HandImportRepository>();
builder.Services.AddScoped<IPreflopSpotRepository, PreflopSpotRepository>();
builder.Services.AddScoped<IHandImportService, HandImportService>();
builder.Services.AddScoped<IPokerHandReader, GgPokerHandReader>();
builder.Services.AddScoped<IPokerHandParser, GgPokerHandParser>();
builder.Services.AddDbContext<PokerTrackerDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PokerDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'PokerDb' is required.");
    }

    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)));
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "/" }
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

var app = builder.Build();

app.MapOpenApi("/api/openapi/{documentName}.json");
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/api/openapi/v1.json", "v1");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PokerTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
