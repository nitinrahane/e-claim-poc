// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Microsoft.OpenApi.Models;
// using Microsoft.IdentityModel.Tokens;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using System.Security.Claims;
// using Newtonsoft.Json.Linq;
// using Microsoft.AspNetCore.Authorization;
// using Serilog;
// using Microsoft.Extensions.Hosting;


// var builder = WebApplication.CreateBuilder(args);


// Log.Logger = new LoggerConfiguration()
//             .ReadFrom.Configuration(Configuration)
//             .Enrich.FromLogContext()
//             .CreateLogger();


// // builder.Host.UseSerilog();

// // Add services to the container.
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Name = "Authorization",
//         Type = SecuritySchemeType.ApiKey,
//         Scheme = "Bearer",
//         BearerFormat = "JWT",
//         In = ParameterLocation.Header,
//         Description = "Enter 'Bearer' followed by a space and the token",
//     });
//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
//             },
//             new string[] {}
//         }
//     });
// });
// // Read allowed origins from appsettings.json
// var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("KongCorsPolicy", builder =>
//     {
//         builder.WithOrigins(allowedOrigins)  // Only allow requests from Kong
//                .AllowAnyMethod()
//                .AllowAnyHeader()
//                .AllowCredentials();
//     });
// });


// // JWT Bearer Authentication with Keycloak
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         // Keycloak Realm and Authority URL
//         options.Authority = "http://keycloak:8080/realms/eclaim-realm"; // Keycloak realm
//         options.RequireHttpsMetadata = false;

//         options.Events = new JwtBearerEvents
//         {
//             OnAuthenticationFailed = context =>
//             {
//                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//                 logger.LogError("Authentication failed.", context.Exception);
//                 if (context.Exception is SecurityTokenExpiredException)
//                 {
//                     logger.LogError("Token has expired.");
//                 }
//                 else if (context.Exception is SecurityTokenInvalidAudienceException)
//                 {
//                     logger.LogError("Invalid audience in the token.");
//                 }
//                 else
//                 {
//                     logger.LogError("Token validation failed: {ExceptionType}", context.Exception.GetType().Name);
//                 }
//                 return Task.CompletedTask;
//             },
//             OnTokenValidated = context =>
//             {
//                 var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
//                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

//                 // Log roles from the token
//                 var realmRoles = context.Principal.FindFirst("realm_access")?.Value;
//                 if (realmRoles != null)
//                 {
//                     logger.LogInformation("Roles in the token: " + realmRoles);
//                 }
//                 else
//                 {
//                     logger.LogWarning("No realm roles found in the token.");
//                 }

//                 return Task.CompletedTask;
//             }
//         };

//         // JWT Token validation parameters
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateAudience = true,
//             ValidAudiences = new[] { "eclaim-api-client", "account" },  // This should match the client ID in Keycloak
//             ValidateIssuer = true,
//             ValidIssuer = "http://localhost:8080/realms/eclaim-realm",  // Keycloak issuer
//             ValidateIssuerSigningKey = true,
//             IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
//             {
//                 // Retrieve the keys from the JWKS URL
//                 var httpClient = new HttpClient();
//                 var jwks = httpClient.GetFromJsonAsync<JsonWebKeySet>("http://keycloak:8080/realms/eclaim-realm/protocol/openid-connect/certs").Result;
//                 return jwks.Keys;
//             }
//         };

//         // OnTokenValidated: Extract roles and log token details
//         options.Events = new JwtBearerEvents
//         {
//             OnTokenValidated = context =>
//             {
//                 var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
//                 var realmRoles = context.Principal.FindFirst("realm_access")?.Value;
//                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

//                 // Log token details
//                 logger.LogInformation("JWT Token validated successfully");

//                 // Extract realm roles from "realm_access" claim
//                 if (realmRoles != null)
//                 {
//                     var roles = JObject.Parse(realmRoles)["roles"].ToObject<string[]>();
//                     foreach (var role in roles)
//                     {
//                         claimsIdentity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role));  // Add role claims
//                         logger.LogInformation($"Adding role claim: {role}");
//                     }
//                 }
//                 else
//                 {
//                     logger.LogWarning("No realm roles found in the token.");
//                 }

//                 return Task.CompletedTask;
//             }
//         };
//     });


// // Add authorization policies
// builder.Services.AddAuthorization(options =>
// {
//     // Admin can access everything
//     options.AddPolicy("AdminPolicy", policy => 
//         policy.RequireRole("Admin"));

//     // Manager can access both Manager and Customer endpoints
//     options.AddPolicy("ManagerPolicy", policy => 
//         policy.RequireRole("Manager", "Admin")); // Admin can access as well

//     // Customer can only access Customer endpoints
//     options.AddPolicy("CustomerPolicy", policy => 
//         policy.RequireRole("Customer", "Manager", "Admin")); // Manager and Admin inherit access
// });

// // Database context setup
// builder.Services.AddDbContext<ClaimDbContext>(options =>
//      options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimDbConnection")));

// // Add services to the container
// builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
// builder.Services.AddScoped<IClaimService, ClaimServiceManager>();
// builder.Services.AddControllers();

// var app = builder.Build();

// // Database migration and seeding
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     var logger = services.GetRequiredService<ILogger<Program>>();
//     try
//     {
//         var context = services.GetRequiredService<ClaimDbContext>();
//         logger.LogInformation("Starting database migration...");
//         context.Database.Migrate(); // Automatically apply migrations
//         logger.LogInformation("Database migration completed.");

//         logger.LogInformation("Starting to seed data...");
//         SeedData.Initialize(services); // Seed data
//         logger.LogInformation("Data seeding completed.");
//     }
//     catch (Exception ex)
//     {
//         logger.LogError(ex, "An error occurred during migration or seeding.");
//     }
// }

// // Configure the HTTP request pipeline
// app.UseSwagger();
// app.UseSwaggerUI();
// app.UseCors("KongCorsPolicy");  // Apply CORS policy

// app.Use(async (context, next) =>
// {
//     var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//     var remoteIp = context.Connection.RemoteIpAddress;
//     var forwardedFor = context.Request.Headers["X-Forwarded-For"];
//     var requestMethod = context.Request.Method;  // Log the request method (GET, POST, OPTIONS, etc.)

//     logger.LogInformation($"Incoming request from {forwardedFor} (IP: {remoteIp}) with method {requestMethod} to {context.Request.Path}");

//     // Log the Authorization header
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

// // Enable authentication and authorization
// app.UseAuthentication();
// app.UseAuthorization();

// // Map controller routes
// app.MapControllers();

// app.Run();


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Sinks.Http; 
using Serilog.Events;
using Serilog.Formatting.Json;


var builder = WebApplication.CreateBuilder(args);

// Serilog configuration for console and Elasticsearch
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)   // Reads configuration from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())                               // Console logging for immediate visibility
    .WriteTo.Debug()                                 // Debug logging for development insights
    .WriteTo.Http("http://logstash:5044", queueLimitBytes: 10000)  // HTTP sink with minimal config
    .CreateLogger();

builder.Host.UseSerilog();

// builder.Host.UseSerilog((context, services, configuration) => configuration
//     .ReadFrom.Configuration(context.Configuration)
//     .Enrich.FromLogContext()
//     .WriteTo.Console(new JsonFormatter()) // JSON console output for Logstash
//     .WriteTo.Http("http://localhost:5044",
//                   queueLimitBytes: null,
//                   batchFormatter: new Serilog.Sinks.Http.BatchFormatters.ArrayBatchFormatter())
// );

// CORS configuration from appsettings.json
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

// Swagger configuration with JWT support
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

// JWT Bearer Authentication with Keycloak
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
                        logger.LogInformation($"Adding role claim: {role}");
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

var app = builder.Build();

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
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var remoteIp = context.Connection.RemoteIpAddress;
    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
    var requestMethod = context.Request.Method;

    logger.LogInformation($"Incoming request from {forwardedFor} (IP: {remoteIp}) with method {requestMethod} to {context.Request.Path}");

    var authHeader = context.Request.Headers["Authorization"].ToString();
    if (!string.IsNullOrEmpty(authHeader))
    {
        logger.LogInformation($"Authorization Token: {authHeader}");
    }
    else
    {
        logger.LogWarning("No Authorization Token found in request.");
    }
    await next.Invoke();
});

app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

app.Run();
