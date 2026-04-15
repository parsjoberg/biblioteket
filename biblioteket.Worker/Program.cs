using biblioteket.Services;
using biblioteket.Services.Interfaces;
using biblioteket.Worker;
using Biblioteket.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("cacheAndMessages");

builder.Services.AddDbContextFactory<BiblioteketDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BiblioteketDb")));

builder.Services.AddSingleton<IBokEventPublisher, NoOpEventPublisher>();
builder.Services.AddScoped<IBokService, BokService>();

builder.Services.AddHostedService<LegimusWorker>();

var host = builder.Build();
host.Run();