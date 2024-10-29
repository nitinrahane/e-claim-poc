using DocumentService.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog.Formatting.Json;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)   // Reads from appsettings.json
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Document API")
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())             // Outputs JSON to the console
    .WriteTo.Http("http://logstash:5044", queueLimitBytes: null) // Sends logs to Logstash
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://keycloak:8080/realms/eclaim-realm";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudiences = new[] { "document-service-client", "account" },
            ValidateIssuer = true,
            ValidIssuer = "http://keycloak:8080/realms/eclaim-realm",
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User", "Admin"));
});


// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(s =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped(s =>
{
    var mongoClient = s.GetRequiredService<IMongoClient>();
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return mongoClient.GetDatabase(settings.DatabaseName);
});

// Register DocumentRepository as a service
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddControllers();
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Document API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public class MongoDbSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}
