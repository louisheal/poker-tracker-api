using System.Data.Common;
using Microsoft.OpenApi;
using MySqlConnector;
using Orleans.Configuration;
using PokerTrackerApi.Database;
using PokerTrackerApi.HandFileSplitting;
using PokerTrackerApi.HandImporting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: true
    );
    builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
}

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("PokerDatabase")
    ?? throw new InvalidOperationException("PokerDatabase connection string is missing.");

builder.Services.AddDatabase(connectionString);

builder.Services.AddHandImporting(builder.Configuration);
builder.Services.AddHandFileSplitting();

DbProviderFactories.RegisterFactory("MySql.Data.MySqlConnector", MySqlConnectorFactory.Instance);

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer(
        (operation, context, cancellationToken) =>
        {
            var description = context.Description;

            if (
                string.Equals(
                    description.HttpMethod,
                    "POST",
                    StringComparison.OrdinalIgnoreCase
                )
                && string.Equals(
                    description.RelativePath?.Trim('/'),
                    "api/imports",
                    StringComparison.OrdinalIgnoreCase
                )
                && operation.RequestBody is { } requestBody
                && requestBody.Content.TryGetValue(
                    "application/x-www-form-urlencoded",
                    out var formContent
                )
            )
            {
                requestBody.Content.Remove("application/x-www-form-urlencoded");
                requestBody.Content["multipart/form-data"] = formContent;
            }

            return Task.CompletedTask;
        }
    );

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

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "poker-tracker-dev";
        options.ServiceId = "poker-tracker";
    });
    siloBuilder.AddAdoNetGrainStorage("Default", options =>
    {
        options.Invariant = "MySql.Data.MySqlConnector";
        options.ConnectionString = connectionString;
    });
});

DatabaseMigrator.Migrate(connectionString);

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

app.Run();
