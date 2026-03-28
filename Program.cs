using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot config
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true);

// Optional: logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Optional: force port
// builder.WebHost.UseUrls("https://localhost:5000");

// Register Ocelot
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Use Ocelot
await app.UseOcelot();

app.Run();