using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Newtonsoft.Json.Linq;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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


// builder.Services.AddAuthentication("Bearer")
//     .AddJwtBearer("Bearer", options =>
//     {
//         options.Authority = "http://localhost:8080/realms/e-claims"; // Keycloak realm
//         options.RequireHttpsMetadata = false;
//         options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//         {
//             ValidateAudience = true,
//             ValidAudience = "account", // Match the 'aud' in the token
//             ValidateIssuer = true,
//             ValidIssuer = "http://localhost:8080/realms/e-claims",
//         };
//     });

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:8080/realms/e-claims";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = "account", // Your client ID
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/e-claims",
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // This will retrieve the keys from the JWKS URL
                var httpClient = new HttpClient();
                var jwks = httpClient.GetFromJsonAsync<JsonWebKeySet>("http://localhost:8080/realms/e-claims/protocol/openid-connect/certs").Result;
                return jwks.Keys;
            }
        };
        // Add your OnTokenValidated logic here
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                var kongRoles = context.Principal.FindFirst("resource_access")?.Value;
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // Log the full token content
                logger.LogInformation("Full token content: {Token}", context.SecurityToken.ToString());
                
                if (kongRoles != null)
                {
                    logger.LogInformation("Extracted resource_access from token: {KongRoles}", kongRoles);

                    var roleClaim = JObject.Parse(kongRoles)["kong"]?["roles"];
                    if (roleClaim != null)
                    {
                        logger.LogInformation("Extracted roles: {Roles}", roleClaim.ToString());
                        var roles = roleClaim.ToObject<string[]>();

                        foreach (var role in roles)
                        {
                            logger.LogInformation("Adding role claim: {Role}", role);
                            claimsIdentity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role));
                        }
                    }
                    else
                    {
                        logger.LogWarning("No roles found under 'kong'.");
                    }
                }
                else
                {
                    logger.LogWarning("No resource_access found in the token.");
                }

                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("user"));
});

builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimServiceManager>();


builder.Services.AddDbContext<ClaimDbContext>(options =>
     options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimDbConnection")));

builder.Services.AddControllers();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ClaimDbContext>();

        logger.LogInformation("Starting database migration...");
        // Automatically apply migrations
        context.Database.Migrate();
        logger.LogInformation("Database migration completed.");

        logger.LogInformation("Starting to seed data...");
        // Seed data
        SeedData.Initialize(services);
        logger.LogInformation("Data seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during migration or seeding.");
    }
}

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

// Logging middleware to track incoming requests
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var remoteIp = context.Connection.RemoteIpAddress;
    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
    logger.LogInformation($"Incoming request from {forwardedFor} (IP: {remoteIp}) to {context.Request.Path}");
    await next.Invoke();
});

app.UseAuthentication();
app.UseAuthorization();
// Map controller routes
app.MapControllers();


app.Run();
