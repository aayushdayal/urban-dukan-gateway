using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot config
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Optional: logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Read JWT settings from configuration
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret is not configured");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is not configured");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is not configured");

// build signing key from base64 secret
var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
var signingKey = new SymmetricSecurityKey(keyBytes);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Bearer", options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5), // adjust as needed
        NameClaimType = "sub"
    };
});

builder.Services.AddAuthorization();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
// Swagger / OpenAPI
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerForOcelot(builder.Configuration);
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ocelot Gateway", Version = "v1" });

//    // JWT Bearer authentication in Swagger
//    var securityScheme = new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Description = "Enter 'Bearer {token}'",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        Reference = new OpenApiReference
//        {
//            Type = ReferenceType.SecurityScheme,
//            Id = "Bearer"
//        }
//    };
//    c.AddSecurityDefinition("Bearer", securityScheme);

//    var securityRequirement = new OpenApiSecurityRequirement
//    {
//        { securityScheme, Array.Empty<string>() }
//    };
//    c.AddSecurityRequirement(securityRequirement);
//});

// Register Ocelot
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.MapGet("/", () => "Ocelot Gateway is running");


// Swagger middleware
//app.UseSwagger();
//app.UseSwaggerForOcelotUI(options =>
//{
//    options.PathToSwaggerGenerator = "/swagger/docs";
//});

// Ensure authentication/authorization run before Ocelot middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"[OCELOT] Incoming: {context.Request.Method} {context.Request.Path}");

    foreach (var header in context.Request.Headers)
    {
        Console.WriteLine($"Header: {header.Key} = {header.Value}");
    }

    await next();

    Console.WriteLine($"[OCELOT] Response Status: {context.Response.StatusCode}");
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

// Use Ocelot
await app.UseOcelot();

app.Run();