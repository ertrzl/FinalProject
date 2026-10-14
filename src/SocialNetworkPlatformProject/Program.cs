using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi.Models;
using Serilog;
using SocialNetworkPlatformProject.Application;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Extensions;
using SocialNetworkPlatformProject.Infrastructure;
using SocialNetworkPlatformProject.Infrastructure.Hubs;
using SocialNetworkPlatformProject.Infrastructure.Services;
using SocialNetworkPlatformProject.Middlewares;
using SocialNetworkPlatformProject.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Logging: what is written, where and how long it is kept comes from the "Serilog" section of appsettings.json
// (the console and a file per day under logs/). Services and middleware just ask for an ILogger<T>.
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// The key that signs login tokens: used as configured, or created once and kept in App_Data (see JwtSecret).
JwtSecret.EnsureConfigured(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

// Add services to the container.
builder.Services.AddControllers().AddMvcOptions(options => LocalizedModelBindingMessages.Apply(options));
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

// One line per request (method, address, status, time). Pages, scripts and pictures are not worth a line each, so they
// (and the health check) are only written at Debug level.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, elapsed, exception) =>
    {
        var path = context.Request.Path;
        var isFile = Path.HasExtension(path) || path.StartsWithSegments("/health") || path.StartsWithSegments(HubRoutes.Prefix);
        if (exception != null || context.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;
        if (isFile)
            return Serilog.Events.LogEventLevel.Debug;
        return context.Response.StatusCode >= 400 ? Serilog.Events.LogEventLevel.Warning : Serilog.Events.LogEventLevel.Information;
    };
});

// The language of the request (Accept-Language, sent by the pages) decides the language of the messages. The languages
// are the ones there is a Locales/<code>.json file for (see Loc), so adding a file adds a language.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(Loc.DefaultLanguage)
    .AddSupportedCultures(Loc.Languages.ToArray())
    .AddSupportedUICultures(Loc.Languages.ToArray()));

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// A mistyped address answers 404 with the site's own page instead of an empty one. Only a page-like address gets it:
// the API, the hubs and files with an extension other than .html (a missing image or script) keep the plain empty 404,
// and so does any 404 that already has a body of its own (this only fills an empty one).
app.UseStatusCodePages(async context =>
{
    var request = context.HttpContext.Request;
    var response = context.HttpContext.Response;
    var path = request.Path;

    var isPage = !path.StartsWithSegments("/api") && !path.StartsWithSegments(HubRoutes.Prefix)
        && (!Path.HasExtension(path) || path.Value!.EndsWith(".html", StringComparison.OrdinalIgnoreCase));
    var page = app.Environment.WebRootFileProvider.GetFileInfo("404.html");
    if (response.StatusCode != StatusCodes.Status404NotFound || !isPage || !page.Exists || !HttpMethods.IsGet(request.Method)) return;

    response.ContentType = "text/html; charset=utf-8";
    response.Headers.CacheControl = "no-cache";
    await response.SendFileAsync(page);
});

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
