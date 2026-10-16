using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi;
using Serilog;
using SocialNetworkPlatformProject.Filters;
using SocialNetworkPlatformProject.Application;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;
using SocialNetworkPlatformProject.Infrastructure;
using SocialNetworkPlatformProject.Infrastructure.Hubs;
using SocialNetworkPlatformProject.Infrastructure.Services;
using SocialNetworkPlatformProject.Middlewares;
using SocialNetworkPlatformProject.Persistence;
using SocialNetworkPlatformProject.Persistence.Extensions;

// The first lines of the log: written before the configuration is read, so a start that fails (a bad setting, a busy
// port) still leaves a trace. UseSerilog below replaces it with the configured logger.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true)
    .CreateBootstrapLogger();

// What nothing else catches: a crash of the whole process, and a background task that failed with nobody waiting for it.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception: the application is stopping");
    Log.CloseAndFlush();
};
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    Log.Error(e.Exception, "A background task failed and nobody was waiting for it");
    e.SetObserved();
};

var builder = WebApplication.CreateBuilder(args);

// Logging: what is written, where and how long it is kept comes from the "Serilog" section of appsettings.json
// (the console and a file per day under logs/). Services and middleware just ask for an ILogger<T>.
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    // The defaults for a line written outside a request (startup, background work): LogContextMiddleware sets the real ones.
    .Enrich.WithProperty("UserId", "-")
    .Enrich.WithProperty("ClientIp", "-"));

// The key that signs login tokens: used as configured, or created once and kept in App_Data (see JwtSecret).
JwtSecret.EnsureConfigured(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

// No "Server: Kestrel" header: it only tells a stranger which software to look for holes in.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// The keys that protect the password-reset links (and anything else ASP.NET Core protects) are kept in a folder, not in
// memory or in a per-user default place: a new start, a new deploy or a move to another machine keeps the links that are
// still waiting valid. "DataProtection:KeysPath" overrides the folder (e.g. a mounted disk on a server).
builder.Services.AddDataProtection()
    .SetApplicationName("SocialNetworkPlatformProject")
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

// Add services to the container.
builder.Services.AddControllers().AddMvcOptions(options =>
{
    LocalizedModelBindingMessages.Apply(options);
    options.Filters.Add<FluentValidationFilter>();
    options.Filters.Add<ActionLogFilter>();
    options.Filters.Add<MediaCookieFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SocialNetworkPlatformProject API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste just the JWT (no 'Bearer ' prefix) — Swagger adds it for you."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// Who is calling (browser, address) for the services that must not touch HttpContext: the list of open sessions.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClientInfo, HttpClientInfo>();

builder.Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

// With "Database:MigrateOnStart" on (the Docker setup), the tables are created or updated before the first request is
// answered. Off by default: see DatabaseMigrationExtensions.
await app.MigrateDatabaseIfRequestedAsync();

// A few facts about this run, so a log starts with what it is about.
Log.Information("Starting in {Environment}. Admin e-mails configured: {AdminCount}. Public address set: {HasPublicAddress}. SMTP configured: {HasSmtp}",
    app.Environment.EnvironmentName,
    builder.Configuration.GetSection("Admin:Emails").Get<string[]>()?.Length ?? 0,
    !string.IsNullOrWhiteSpace(builder.Configuration["App:PublicBaseUrl"]),
    !string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Host"]));

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
        // A request that took over two seconds is worth a look even when it worked.
        return context.Response.StatusCode >= 400 || elapsed > 2000 ? Serilog.Events.LogEventLevel.Warning : Serilog.Events.LogEventLevel.Information;
    };

    // Who, from where and with what: the line is written after the request, when the user is known.
    options.EnrichDiagnosticContext = (diagnostic, context) =>
    {
        diagnostic.Set("UserId", RequestLogProperties.UserId(context));
        diagnostic.Set("ClientIp", RequestLogProperties.ClientIp(context));
        diagnostic.Set("UserAgent", context.Request.Headers.UserAgent.ToString());
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

// Before the files: a page, a script or a picture asked for over plain http is sent to https as well (it used to be served
// as it was, because the redirect came after the static files).
app.UseHttpsRedirection();

app.UseDefaultFiles();

// /uploads is not a static folder: any file in it would be handed to anybody. UploadsController answers it instead, and
// asks who is looking (see MediaCookie).
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/uploads"), files => files.UseStaticFiles(new StaticFileOptions
{
    // The pages and scripts share helpers (api.js, app.js...) and change often. Without an explicit header the
    // browser decides on its own how long to keep them, so a user could run a new page against an old helper
    // file and hit "x is not defined". "no-cache" means: always ask the server, which answers 304 (no body)
    // while the file is unchanged.
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name);
        if (extension is ".js" or ".css" or ".html")
            context.Context.Response.Headers.CacheControl = "no-cache";
    }
}));

app.UseAuthentication();
app.UseAuthorization();

// After authentication, so the user is known: from here on every log line carries the user and the address.
app.UseMiddleware<LogContextMiddleware>();

// After authentication, so the general limit can count per signed-in user.
app.UseRateLimiter();

// Every controller action needs a signed-in person unless it says [AllowAnonymous] (sign-in, registration, password reset). A
// controller added later without [Authorize] is closed, not silently public. (Done here and not as a fallback policy: a
// fallback policy also answers 401 to every address that does not exist, and a mistyped address must get the 404 page.)
app.MapControllers().RequireAuthorization();

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
