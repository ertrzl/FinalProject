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
        var jwtSecret = JwtSecret.Read(configuration);

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddHttpClient<IMusicSearchService, ItunesMusicSearchService>(client => client.Timeout = TimeSpan.FromSeconds(8));

        services.AddMemoryCache();
        services.AddSingleton<HubConnectionRegistry>();
        services.AddScoped<AccessTokenValidator>();
        services.AddScoped<IAccessTokenRevoker>(provider => provider.GetRequiredService<AccessTokenValidator>());

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender>(provider =>
        {
            if (provider.GetRequiredService<IOptions<EmailOptions>>().Value.Smtp.IsConfigured)
                return ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider);

            return provider.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? ActivatorUtilities.CreateInstance<ConsoleEmailSender>(provider)
                : ActivatorUtilities.CreateInstance<DisabledEmailSender>(provider);
        });

        services.AddHostedService<MaintenanceBackgroundService>();

        services.AddHostedService<PostMentionBackfillService>();

        services.AddSignalR(options => options.AddFilter<SecurityStampHubFilter>());
        services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
        services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

        services.AddSingleton<PresenceTracker>();
        services.AddSingleton<IPresenceTracker>(sp => sp.GetRequiredService<PresenceTracker>());

        TokenValidationParameters Parameters(string? audience) => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ClockSkew = TimeSpan.FromSeconds(30),

            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

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
            options.MapInboundClaims = false;
            options.TokenValidationParameters = Parameters(configuration["Jwt:Audience"]);

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
