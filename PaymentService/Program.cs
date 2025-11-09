using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentService.Configuration;
using PaymentService.Data;
using PaymentService.Events.Publishers;
using PaymentService.Middlewares;
using PaymentService.Services;
using PaymentService.Services.Providers;
using PaymentService.Utils;
using System.Text;
using System.Text.Json.Serialization;
using StripeProvider = PaymentService.Services.Providers.StripePaymentProvider;
using PayPalProvider = PaymentService.Services.Providers.PayPalPaymentProvider;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddDapr() // Add Dapr integration
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original property names
    });

// Add rate limiting services
builder.Services.AddRateLimitingServices(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Payment Service API", 
        Version = "v1",
        Description = "Payment processing service with support for multiple payment providers (Stripe, PayPal, Square)"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configuration
builder.Services.Configure<PaymentSettings>(
    builder.Configuration.GetSection("PaymentSettings"));
builder.Services.Configure<PaymentProvidersSettings>(
    builder.Configuration.GetSection("PaymentProviders"));

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is required"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Application Services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IStandardLogger, StandardLogger>();
builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();

// Dapr Services
builder.Services.AddSingleton<DaprEventPublisher>();
builder.Services.AddSingleton<PaymentService.Services.DaprSecretService>();

// Payment Providers
builder.Services.AddScoped<StripeProvider>();
builder.Services.AddScoped<PayPalProvider>();
// Square provider temporarily disabled due to SDK compatibility issues
// builder.Services.AddScoped<SquarePaymentProvider>();
builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
        c.RoutePrefix = string.Empty; // Make Swagger UI the root page
    });
}

// Middleware pipeline
app.UseCorrelationId();
app.UsePaymentServiceRateLimiting(builder.Configuration);
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Health checks mapped through operational controller
// app.MapHealthChecks("/health"); // Replaced with operational controller

// Enable Dapr CloudEvents and subscribe handler
app.UseCloudEvents();
app.MapSubscribeHandler();

// Controllers
app.MapControllers();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Apply pending migrations
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

app.Logger.LogInformation("Payment Service started successfully");

app.Run();
