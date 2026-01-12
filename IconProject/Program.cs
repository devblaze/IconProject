using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using IconProject.Configuration;
using IconProject.Database;
using IconProject.Database.Repositories;
using IconProject.Database.Repositories.Interfaces;
using IconProject.Database.UnitOfWork;
using IconProject.Middleware;
using IconProject.Services;
using IconProject.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =================================
// Configuration
// =================================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing");

// =================================
// CORS Configuration
// =================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Vite dev server
                "http://localhost:3000",  // Docker frontend
                "http://frontend:80"      // Docker network
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// =================================
// Add Controllers
// =================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =================================
// Swagger with JWT Support
// =================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description = "A Task Management System API with JWT Authentication"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// =================================
// JWT Authentication
// =================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// =================================
// Database Configuration
// =================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =================================
// Register Unit of Work Pattern
// =================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Generic Repository (for direct repository access if needed)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// =================================
// Register Application Services
// =================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

// =================================
// Global Exception Handling Middleware (must be first)
// =================================
app.UseGlobalExceptionHandler();

// =================================
// Migrate Automatically at Startup
// =================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var retries = 2;
    var retryDelay = TimeSpan.FromSeconds(5);
    var migrated = false;

    for (int attempt = 1; attempt <= retries && !migrated; attempt++)
    {
        try
        {
            logger.LogInformation("Attempting to migrate database. Attempt: {Attempt}/{Retries}", attempt, retries);
            dbContext.Database.Migrate();
            logger.LogInformation("Database migration successful.");
            migrated = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration failed. Waiting and retrying again...");
            Thread.Sleep(retryDelay);
        }
    }

    if (!migrated)
    {
        logger.LogCritical("Database migration failed after all attempts.");
    }
}

// =================================
// Configure HTTP Request Pipeline
// =================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// CORS must be before Authentication
app.UseCors("AllowFrontend");

// Authentication & Authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
