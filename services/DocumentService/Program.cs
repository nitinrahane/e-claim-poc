using DocumentService.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog.Formatting.Json;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using DocumentService.Messaging;
using EClaim.Shared.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)   // Reads from appsettings.json
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Document API")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")          
    .WriteTo.Http("http://logstash:5044", queueLimitBytes: null) // Sends logs to Logstash
    .CreateLogger();

builder.Host.UseSerilog();

// Add CORS policy (assuming the configuration exists)
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("KongCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure authentication and authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://keycloak:8080/realms/eclaim-realm";
        options.RequireHttpsMetadata = false;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudiences = new[] { "eclaim-api-client", "account" },
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/eclaim-realm",
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var httpClient = new HttpClient();
                var jwks = httpClient.GetFromJsonAsync<JsonWebKeySet>("http://keycloak:8080/realms/eclaim-realm/protocol/openid-connect/certs").Result;
                return jwks.Keys;
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Authentication failed.", context.Exception);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                var realmRoles = context.Principal.FindFirst("realm_access")?.Value;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("JWT Token validated successfully");
                if (realmRoles != null)
                {
                    var roles = JObject.Parse(realmRoles)["roles"].ToObject<string[]>();
                    foreach (var role in roles)
                    {
                        claimsIdentity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role));
                        // logger.LogInformation($"Adding role claim: {role}");
                    }
                }
                else
                {
                    logger.LogWarning("No realm roles found in the token.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerPolicy", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("Customer", "Manager", "Admin"));
});

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
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

// Configure RabbitMQ connection with retry logic
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("RabbitMQ");
    var factory = new ConnectionFactory() 
    { 
        HostName = config["HostName"],
        DispatchConsumersAsync = true // Enables async consumer support
    };
    
    IConnection connection = null;
    int retryCount = 5; // Maximum number of retries
    int delay = 2000; // Delay in milliseconds between retries

    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            connection = factory.CreateConnection();
            Log.Information("RabbitMQ connection established.");
            break; // Exit loop if connection is successful
        }
        catch (Exception ex)
        {
            Log.Warning($"RabbitMQ connection attempt {i + 1} failed: {ex.Message}");
            Task.Delay(delay).Wait(); // Wait before retrying
        }
    }

    if (connection == null)
    {
        throw new Exception("Failed to connect to RabbitMQ after multiple attempts.");
    }

    return connection;
});

// Register DocumentCreatedSubscriber with dependencies
builder.Services.AddSingleton<IEventSubscriber, DocumentCreatedSubscriber>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>(); // Use scope factory instead
    var logger = sp.GetRequiredService<ILogger<DocumentCreatedSubscriber>>();
    var config = sp.GetRequiredService<IConfiguration>();

    return new DocumentCreatedSubscriber(connection, scopeFactory, logger, config);
});

// Register DocumentRepository as a service
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddControllers();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
 

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Document API", Version = "v1" });

    // Add security definitions for JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and the token",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });

    // Register the custom operation filter for file uploads
    c.OperationFilter<FileUploadOperationFilter>();
});

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// Start RabbitMQ listener
var subscriber = app.Services.GetRequiredService<IEventSubscriber>();
subscriber.StartListening();

app.UseAuthorization();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document API V1");
});
app.UseCors("KongCorsPolicy");
app.Run();

public class MongoDbSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile));

        if (fileParameters.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
        }
    }
}
