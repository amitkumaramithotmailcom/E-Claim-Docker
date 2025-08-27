using E_Claim_Service;
using EClaim.Domain.Interfaces;
using EClaim.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>()
                .AddScoped<IClaimService, ClaimService>()
                .AddScoped<IClaimService, ClaimService>()
                .AddScoped<IUserService, UserService>()
                 .AddScoped<ICacheUtility, CacheUtility>()
                .AddScoped<IAppSettingsService, AppSettingsService>(); 

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register default logger to DB
var serviceProvider = builder.Services.BuildServiceProvider();
var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
builder.Logging.ClearProviders()
               .AddConsole()
               .AddProvider(new DbLoggerProvider(scopeFactory));

// Register Redis connection multiplexer
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();   // ensures tables like Users, Logs are created
    await DbSeeder.SeedUsersAsync(dbContext); // safe to run after Migrate()
}

app.UseMiddleware<GlobalExceptionMiddleware>(); // 1. Custom error handling — always FIRST

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger()        // 2. Swagger generation
       .UseSwaggerUI();     // 3. Swagger UI (only in dev)
}

app.UseHttpsRedirection()   // 4. Redirect HTTP to HTTPS
   .UseRouting()            // 5. Enables routing (MUST come before auth)
   .UseAuthentication()     // 6. Authenticate user
   .UseAuthorization();     // 7. Authorize access

app.MapControllers();       // 8. Maps controller endpoints

app.Run();
