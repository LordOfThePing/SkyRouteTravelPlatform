using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkyRoute.Api.DTOs;
using SkyRoute.Api.Middleware;
using SkyRoute.Api.Validators;
using SkyRoute.Infrastructure.Extensions;
using SkyRoute.Infrastructure.Persistence;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()));

// Resolve the SQLite file to <repo>/backend/data/skyroute.db so the source tree
// (backend/src/SkyRoute.Api) stays clean. ContentRootPath = the API project folder.
var dataDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data"));
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "skyroute.db");
builder.Configuration["ConnectionStrings:Default"] = $"Data Source={dbPath}";

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IValidator<SearchRequestDto>, SearchRequestValidator>();
builder.Services.AddScoped<IValidator<BookingRequestDto>, BookingRequestValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SkyRoute API", Version = "v1" });
});

builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("search", cfg =>
    {
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.PermitLimit = 60;
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    opts.AddFixedWindowLimiter("booking", cfg =>
    {
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.PermitLimit = 20;
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SkyRouteDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithTags("Health");

app.MapControllers();

app.Run();

public partial class Program { }
