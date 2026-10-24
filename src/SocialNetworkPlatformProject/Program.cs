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

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true)
    .CreateBootstrapLogger();

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

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("UserId", "-")
    .Enrich.WithProperty("ClientIp", "-"));

JwtSecret.EnsureConfigured(builder.Configuration, Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddDataProtection()
    .SetApplicationName("SocialNetworkPlatformProject")
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClientInfo, HttpClientInfo>();

builder.Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiRateLimiting(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseIfRequestedAsync();

Log.Information("Starting in {Environment}. Admin e-mails configured: {AdminCount}. Public address set: {HasPublicAddress}. SMTP configured: {HasSmtp}",
    app.Environment.EnvironmentName,
    builder.Configuration.GetSection("Admin:Emails").Get<string[]>()?.Length ?? 0,
    !string.IsNullOrWhiteSpace(builder.Configuration["App:PublicBaseUrl"]),
    !string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Host"]));

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
        return context.Response.StatusCode >= 400 || elapsed > 2000 ? Serilog.Events.LogEventLevel.Warning : Serilog.Events.LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnostic, context) =>
    {
        diagnostic.Set("UserId", RequestLogProperties.UserId(context));
        diagnostic.Set("ClientIp", RequestLogProperties.ClientIp(context));
        diagnostic.Set("UserAgent", context.Request.Headers.UserAgent.ToString());
    };
});

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(Loc.DefaultLanguage)
    .AddSupportedCultures(Loc.Languages.ToArray())
    .AddSupportedUICultures(Loc.Languages.ToArray()));

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

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
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseMiddleware<CleanUrlMiddleware>();

app.UseDefaultFiles();

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/uploads"), files => files.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name);
        if (extension is ".js" or ".css" or ".html")
            context.Context.Response.Headers.CacheControl = "no-cache";
    }
}));

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LogContextMiddleware>();

app.UseRateLimiter();

app.MapControllers().RequireAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapHub<NotificationsHub>(HubRoutes.Notifications);
app.MapHub<MessagesHub>(HubRoutes.Messages);

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
    }
}
