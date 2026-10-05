using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi.Models;
using SocialNetworkPlatformProject.Application;
using SocialNetworkPlatformProject.Extensions;
using SocialNetworkPlatformProject.Infrastructure;
using SocialNetworkPlatformProject.Infrastructure.Hubs;
using SocialNetworkPlatformProject.Middlewares;
using SocialNetworkPlatformProject.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SocialNetworkPlatformProject API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste just the JWT (no 'Bearer ' prefix) — Swagger adds it for you.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });
});

builder.Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Tells browsers to use HTTPS only for this site from now on.
    app.UseHsts();
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    // The pages and scripts share helpers (api.js, app.js...) and change often. Without an explicit header the
    // browser decides on its own how long to keep them, so a user could run a new page against an old helper
    // file and hit "x is not defined". "no-cache" means: always ask the server, which answers 304 (no body)
    // while the file is unchanged. Uploaded media keeps the default (its file names never change).
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name);
        if (extension is ".js" or ".css" or ".html")
            context.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// After authentication, so the general limit can count per signed-in user.
app.UseRateLimiter();

app.MapControllers();

// For monitors and load balancers: 200 "Healthy" when the database answers, 503 otherwise.
app.MapHealthChecks("/health").AllowAnonymous();

app.MapHub<NotificationsHub>(HubRoutes.Notifications);
app.MapHub<MessagesHub>(HubRoutes.Messages);

// Dev convenience: launchSettings.json can only auto-open one URL, so open both the site and Swagger here instead.
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var baseUrl = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

        if (baseUrl == null)
            return;

        OpenBrowser(baseUrl);
        OpenBrowser($"{baseUrl}/swagger");
    });
}

app.Run();

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch
    {
        // Best-effort only — no browser available (e.g. running headless) shouldn't crash startup.
    }
}
