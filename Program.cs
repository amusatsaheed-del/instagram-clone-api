using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InstagramClone.Api.Data;
using InstagramClone.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== Logging =====
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.WriteTo.Console()
        .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
        .MinimumLevel.Information());

// ===== Database Configuration =====
var accountEndpoint = builder.Configuration["Database:Cosmos:AccountEndpoint"];
var accountKey = builder.Configuration["Database:Cosmos:AccountKey"];
var databaseName = builder.Configuration["Database:Cosmos:DatabaseName"];

if (string.IsNullOrWhiteSpace(accountEndpoint) ||
    string.IsNullOrWhiteSpace(accountKey) ||
    string.IsNullOrWhiteSpace(databaseName))
{
    throw new InvalidOperationException("Cosmos DB configuration is missing. Set Database:Cosmos:AccountEndpoint, Database:Cosmos:AccountKey, and Database:Cosmos:DatabaseName.");
}

builder.Services.AddDbContext<InstagramContext>(options =>
{
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    options.UseCosmos(accountEndpoint, accountKey, databaseName);
});

// ===== Authentication & Authorization =====
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ??
    "your-256-bit-secret-key-must-be-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "instagram-clone";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "instagram-clone-users";

builder.Services.AddSingleton(new JwtRuntimeSettings
{
    SecretKey = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// ===== Business Services =====
builder.Services.AddScoped<AuthService>();

// ===== API Configuration =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Instagram Clone API",
        Version = "v1",
        Description = "Cloud-native media sharing platform API"
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter your JWT token",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "" } }
    });
});

// ===== CORS Configuration =====
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy("FrontendCors", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ===== Response Compression =====
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// ===== Application Insights =====
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// ===== Database Migration =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<InstagramContext>();
        await db.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Document database initialized successfully using provider {provider}", db.Database.ProviderName);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database initialization failed");
    }
}

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseResponseCompression();

app.UseCors("FrontendCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Logger.LogInformation("Application starting on {timestamp} with database provider cosmos", DateTime.UtcNow);
await app.RunAsync();
