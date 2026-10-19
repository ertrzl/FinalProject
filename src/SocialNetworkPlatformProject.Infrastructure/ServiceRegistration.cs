using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Infrastructure.Email;
using SocialNetworkPlatformProject.Infrastructure.Hubs;
using SocialNetworkPlatformProject.Infrastructure.Services;

namespace SocialNetworkPlatformProject.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail at startup, not on the first login, when the signing key was never configured on this machine.
        var jwtSecret = JwtSecret.Read(configuration);

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddHttpClient<IMusicSearchService, ItunesMusicSearchService>(client => client.Timeout = TimeSpan.FromSeconds(8));

        services.AddMemoryCache();
        services.AddSingleton<HubConnectionRegistry>();
        services.AddScoped<AccessTokenValidator>();
        services.AddScoped<IAccessTokenRevoker>(provider => provider.GetRequiredService<AccessTokenValidator>());

        // E-mail goes through the SMTP account in the settings when there is one. Without one, Development writes the
        // message to the console (so the flow works with no account) and everywhere else it is dropped with a warning:
        // a password reset link in a log file would let anyone who can read the log take over accounts.
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender>(provider =>
        {
            if (provider.GetRequiredService<IOptions<EmailOptions>>().Value.Smtp.IsConfigured)
                return ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider);

            return provider.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? ActivatorUtilities.CreateInstance<ConsoleEmailSender>(provider)
                : ActivatorUtilities.CreateInstance<DisabledEmailSender>(provider);
        });

        // Timer-driven housekeeping: event reminders, waiting lists and expired stories.
        services.AddHostedService<MaintenanceBackgroundService>();

        services.AddSignalR(options => options.AddFilter<SecurityStampHubFilter>());
        services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
        services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

        services.AddSingleton<PresenceTracker>();
        services.AddSingleton<IPresenceTracker>(sp => sp.GetRequiredService<PresenceTracker>());

        // The two kinds of token differ in audience only (see MediaCookie): the API's token is refused for uploads and the
        // photo cookie's token is refused by the API.
        TokenValidationParameters Parameters(string? audience) => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // The default allows a token to be used five minutes after it expired; with a 15-minute life that
            // would be a third more. The page renews its token 30 seconds early anyway.
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        // A token outlives the account it was issued for, the password it was issued under and the sign-out that ended it:
        // refuse it once the user is gone, the security stamp in it is no longer the user's current one, or it was signed out.
        static async Task RefuseRevokedAsync(TokenValidatedContext context)
        {
            var sub = context.Principal?.FindFirst("sub")?.Value;
            var stamp = context.Principal?.FindFirst(TokenService.SecurityStampClaim)?.Value;
            var tokenId = context.Principal?.FindFirst("jti")?.Value;
            var sessionId = context.Principal?.FindFirst(TokenService.SessionClaim)?.Value;
            var validator = context.HttpContext.RequestServices.GetRequiredService<AccessTokenValidator>();

            if (!Guid.TryParse(sub, out var userId) || !await validator.IsValidAsync(userId, stamp, tokenId, sessionId))
                context.Fail("The token is no longer valid.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            // Keep claim names exactly as TokenService wrote them ("sub" stays "sub").
            options.MapInboundClaims = false;
            options.TokenValidationParameters = Parameters(configuration["Jwt:Audience"]);

            // Browsers can't set an Authorization header on a WebSocket, so SignalR sends the JWT as ?access_token=.
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = RefuseRevokedAsync,

                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(HubRoutes.Prefix))
                        context.Token = accessToken;

                    return Task.CompletedTask;
                }
            };
        }).AddJwtBearer(MediaCookie.Scheme, options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = Parameters(TokenService.MediaAudience(configuration));

            // An <img> tag cannot send a header: the token comes from the cookie.
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = RefuseRevokedAsync,

                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies[MediaCookie.Name];
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
