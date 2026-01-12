using Microsoft.EntityFrameworkCore;
using IconProject.Database;
using IconProject.Database.Repositories;
using IconProject.Database.Repositories.Interfaces;
using IconProject.Database.UnitOfWork;
using IconProject.Middleware;
using IconProject.Services;
using IconProject.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Unit of Work pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Generic Repository (for direct repository access if needed)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Register application services
builder.Services.AddScoped<ITaskService, TaskService>();
// builder.Services.AddScoped<IUserService, UserService>();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
// app.UseAuthorization();
app.MapControllers();

app.Run();
