using Microsoft.EntityFrameworkCore;
using IconProject.Database;
using IconProject.Database.Repositories;
using IconProject.Database.Repositories.Interfaces;
using IconProject.Services;
using IconProject.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
// builder.Services.AddHttpClient<IApiClient, ApiClient>();
builder.Services.AddScoped<ICatService, CatService>();

var app = builder.Build();

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