using Amazon.Runtime;
using Amazon.S3;
using Microsoft.OpenApi;
using Orleans.Configuration;
using PokerTrackerApi.Database;
using PokerTrackerApi.HandImporting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("PokerDatabase")
    ?? throw new InvalidOperationException("PokerDatabase connection string is missing.");

builder.Services.AddDatabase(connectionString);

builder.Services.AddHandImporting();

var importStorageProvider = builder.Configuration["HandImportStorage:Provider"] ?? "Local";
if (string.Equals(importStorageProvider, "Garage", StringComparison.OrdinalIgnoreCase))
{
    var endpoint =
        builder.Configuration["HandImportStorage:Endpoint"] ?? "http://localhost:3900";

    var region = builder.Configuration["HandImportStorage:Region"] ?? "garage";
    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var serviceUri)
        || serviceUri.Scheme is not ("http" or "https"))
    {
        throw new InvalidOperationException(
            "HandImportStorage:Endpoint must be an absolute HTTP or HTTPS URL."
        );
    }

    var s3Config = new AmazonS3Config
    {
        ServiceURL = endpoint,
        ForcePathStyle = true,
        AuthenticationRegion = region,
    };
    var accessKey = builder.Configuration["HandImportStorage:AccessKey"];
    var secretKey = builder.Configuration["HandImportStorage:SecretKey"];
    if (string.IsNullOrWhiteSpace(accessKey) != string.IsNullOrWhiteSpace(secretKey))
    {
        throw new InvalidOperationException(
            "HandImportStorage:AccessKey and HandImportStorage:SecretKey must be configured together."
        );
    }

    if (!string.IsNullOrWhiteSpace(accessKey))
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        builder.Services.AddSingleton<IAmazonS3>(_ =>
            new AmazonS3Client(credentials, s3Config)
        );
    }
    else
    {
        builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(s3Config));
    }

    builder.Services.AddSingleton<IHandFileStore>(serviceProvider =>
        new GarageHandFileStore(
            serviceProvider.GetRequiredService<IAmazonS3>()
        )
    );
}
else
{
    throw new InvalidOperationException(
        $"Unsupported hand import storage provider '{importStorageProvider}'."
    );
}

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
