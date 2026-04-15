using biblioteket.Services;
using biblioteket.Services.Interfaces;
using biblioteket.Web;
using biblioteket.Web.Components;
using Biblioteket.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<BiblioteketDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BiblioteketDb")));

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cacheAndMessages");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//    {
//        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
//        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
//        client.BaseAddress = new("https+http://apiservice");
//    });

builder.Services.AddSingleton<IBokEventPublisher, RedisEventPublisher>();
builder.Services.AddScoped<IBokService, BokService>();

var app = builder.Build();

// Ensure database exists and all migrations are applied on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BiblioteketDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();    
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
