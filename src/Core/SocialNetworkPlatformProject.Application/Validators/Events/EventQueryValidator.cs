using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class EventQueryValidator : AbstractValidator<EventQuery>
{
    public const int MaxSearchLength = 100;

    public EventQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(MaxSearchLength);

        RuleFor(x => x.When)
            .Must(when => EventDateRange.All.Contains(when!))
            .When(x => !string.IsNullOrEmpty(x.When))
            .WithMessage("When must be one of: " + string.Join(", ", EventDateRange.All));

        RuleFor(x => x.TimeZone)
            .Must(zone => EventDateRange.TryResolveZone(zone, out _))
            .WithMessage("Saat dilimi tanınmıyor.");

        RuleFor(x => x)
            .Must(x => x.From!.Value.ToUtc() <= x.To!.Value.ToUtc())
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}
