using biblioteket.Services;
using biblioteket.Services.Interfaces;
using Biblioteket.Data;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddProblemDetails();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();


        builder.Services.AddDbContextFactory<BiblioteketDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("BiblioteketDb")));

        builder.AddRedisClient("cacheAndMessages");

        builder.Services.AddSingleton<IBokEventPublisher, RedisEventPublisher>();
        builder.Services.AddScoped<IBokService, BokService>();


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapGet("/", () => "API service is running.");

        app.MapPost("api/legimusUrl", async (LegimusUrlRequest request, IBokService bokService) =>
        {
            await bokService.LäggTillBokAsync(request.LegimusUrl);
            return Results.Created();
        })
        .WithName("AddLegimusUrl");

        app.MapDefaultEndpoints();

        app.Run();
    }
}

public record LegimusUrlRequest(string LegimusUrl);