using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PokerTrackerApi.Diagnostics;
using PokerTrackerApi.HandAnnotations;
using PokerTrackerApi.HandHistories;
using PokerTrackerApi.HandImporting;
using PokerTrackerApi.HandImporting.HandParsers;
using PokerTrackerApi.HandImporting.HandReaders;
using PokerTrackerApi.HandImporting.HandReprocessing;
using PokerTrackerApi.HandImporting.Jobs;
using PokerTrackerApi.HandReplays;
using PokerTrackerApi.Metrics;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;
using PokerTrackerApi.Winrate;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IHandHistoryRepository, HandHistoryRepository>();
builder.Services.AddScoped<IWinrateRepository, WinrateRepository>();
builder.Services.AddScoped<IMetricsRepository, MetricsRepository>();
builder.Services.AddScoped<IRiverDiagnosticsRepository, RiverDiagnosticsRepository>();
builder.Services.AddScoped<IHandReplayRepository, HandReplayRepository>();
builder.Services.AddScoped<IHandAnnotationsRepository, HandAnnotationsRepository>();
builder.Services.AddScoped<IPreflopSpotRepository, PreflopSpotRepository>();
builder.Services.AddScoped<IHandReprocessingRepository, HandReprocessingRepository>();
builder.Services.AddScoped<IHandImportRepository, HandImportRepository>();
builder.Services.AddScoped<IHandImportJobRepository, HandImportJobRepository>();

builder.Services.AddScoped<IHandReprocessingService, HandReprocessingService>();
builder.Services.AddScoped<IHandHistoryMapper, HandHistoryMapper>();
builder.Services.AddScoped<IHandImportService, HandImportService>();
builder.Services.AddScoped<IHandImportJobService, HandImportJobService>();
builder.Services.AddScoped<IPokerHandReader, GgPokerHandReader>();
builder.Services.AddScoped<IPokerHandParser, GgPokerHandParser>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDbContext<PokerTrackerDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PokerDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'PokerDb' is required.");
    }

    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)));
});

var importStagingDirectory =
    builder.Configuration["HandImportStorage:Directory"]
    ?? Path.Combine(AppContext.BaseDirectory, "import-staging");
builder.Services.AddSingleton<IHandImportFileStore>(
    new HandImportFileStore(importStagingDirectory)
);
builder.Services.AddHostedService<HandImportBackgroundService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Servers = new List<OpenApiServer> { new() { Url = "/" } };

            return Task.CompletedTask;
        }
    );
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
