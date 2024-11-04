// using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Serilog;
// using Serilog.Events;
using Serilog.Formatting.Json;
using RabbitMQ.Client;
using EClaim.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for JSON logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)   // Reads from appsettings.json
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Claims API")
    .WriteTo.Console(new JsonFormatter())             // Outputs JSON to the console
    .WriteTo.Http("http://logstash:5044", queueLimitBytes: null) // Sends logs to Logstash
    .CreateLogger();

builder.Host.UseSerilog();

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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
});

// JWT Bearer Authentication setup
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

// Authorization policies for role-based access
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerPolicy", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("Customer", "Manager", "Admin"));
});

// Database context setup
builder.Services.AddDbContext<ClaimDbContext>(options =>
     options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimDbConnection")));

// Add custom services to the DI container
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimServiceManager>();
builder.Services.AddControllers();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>().GetSection("RabbitMQ");
    var factory = new ConnectionFactory() { HostName = config["HostName"] };
    return factory.CreateConnection();
});

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();
    var config = sp.GetRequiredService<IConfiguration>().GetSection("RabbitMQ");

    // Declare the exchange
    channel.ExchangeDeclare(exchange: config["Exchange"], type: ExchangeType.Direct, durable: true);

    // Declare the queue and bind it to the exchange with a routing key
    var queueName = config["Queue"];        // Get the queue name from configuration
    var routingKey = config["RoutingKey"];      // Get the routing key from configuration

    channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
    channel.QueueBind(queue: queueName, exchange: config["Exchange"], routingKey: routingKey);

    return new RabbitMqEventPublisher(channel);
});


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ClaimDbContext>();
        logger.LogInformation("Starting database migration...");
        context.Database.Migrate();
        logger.LogInformation("Database migration completed.");

        logger.LogInformation("Starting to seed data...");
        SeedData.Initialize(services);
        logger.LogInformation("Data seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during migration or seeding.");
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("KongCorsPolicy");

// Request logging middleware
// app.Use(async (context, next) =>
// {
//     var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//     var remoteIp = context.Connection.RemoteIpAddress;
//     var forwardedFor = context.Request.Headers["X-Forwarded-For"];
//     var requestMethod = context.Request.Method;

//     logger.LogInformation($"Incoming request from {forwardedFor} (IP: {remoteIp}) with method {requestMethod} to {context.Request.Path}");

//     var authHeader = context.Request.Headers["Authorization"].ToString();
//     if (!string.IsNullOrEmpty(authHeader))
//     {
//         logger.LogInformation($"Authorization Token: {authHeader}");
//     }
//     else
//     {
//         logger.LogWarning("No Authorization Token found in request.");
//     }
//     await next.Invoke();
// });

app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

app.Run();
