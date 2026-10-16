using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SocialNetworkPlatformProject.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper 15 and later asks for a license key and logs a warning at every start without one. The free Community key
        // (luckypennysoftware.com) goes into the setting "AutoMapper:LicenseKey" (appsettings, user secrets or an environment
        // variable); with no key the app runs the same, only the warning stays.
        services.AddAutoMapper(
            (serviceProvider, cfg) => cfg.LicenseKey = serviceProvider.GetService<IConfiguration>()?["AutoMapper:LicenseKey"],
            Assembly.GetExecutingAssembly());

        // FluentValidation's built-in texts ("must not be empty"...) follow the request's language by themselves (it has
        // Turkish and English); the rules that say something more specific carry their own message (WithMessage + Loc).
        // The validators run on every request through FluentValidationFilter in the web project.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
