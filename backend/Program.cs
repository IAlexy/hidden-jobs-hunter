using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using HiddenJobsHunter.Api.Data;
using HiddenJobsHunter.Api.Providers;
using HiddenJobsHunter.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database: SQLite
var defaultDbPath = Path.Combine(builder.Environment.ContentRootPath, "hiddenjobs.db");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Data Source={defaultDbPath}";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Http Clients
builder.Services.AddHttpClient();

// Providers
builder.Services.AddHttpClient<HackerNewsProvider>();
builder.Services.AddHttpClient<RemotiveProvider>();
builder.Services.AddHttpClient<JobicyProvider>();
builder.Services.AddHttpClient<CustomAtsProvider>();

builder.Services.AddScoped<IJobProvider, HackerNewsProvider>(sp => sp.GetRequiredService<HackerNewsProvider>());
builder.Services.AddScoped<IJobProvider, RemotiveProvider>(sp => sp.GetRequiredService<RemotiveProvider>());
builder.Services.AddScoped<IJobProvider, JobicyProvider>(sp => sp.GetRequiredService<JobicyProvider>());
builder.Services.AddScoped<IJobProvider, CustomAtsProvider>(sp => sp.GetRequiredService<CustomAtsProvider>());

// Services
builder.Services.AddHttpClient<IAiMatcherService, AiMatcherService>();
builder.Services.AddScoped<JobSyncService>();

// CORS for local React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-migrate & Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    db.SeedDefaults();
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhost");

app.MapControllers();

await app.RunAsync();

public partial class Program
{
    protected Program()
    {
    }
}
