using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

// Rules shared by creating and editing an event.
public abstract class EventInputValidator<T> : AbstractValidator<T> where T : EventInputDto
{
    protected EventInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => Loc.T("val.event.title"))
            .MaximumLength(150);

        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);

        // The map pin: both numbers or none, latitude -90..90 and longitude -180..180. An online event has no pin, so
        // whatever came with it is ignored (EventService drops it) and not checked.
        RuleFor(x => x.Latitude)
            .Must((dto, latitude) => EventCoordinates.TryParse(latitude, dto.Longitude, out _, out _))
            .WithMessage(_ => Loc.T("val.event.coordinates"))
            .When(x => !x.IsOnline);

        RuleFor(x => x.EndsAt!.Value)
            .Must((dto, endsAt) => endsAt.ToUtc() > dto.StartsAt.ToUtc()).WithMessage(_ => Loc.T("val.event.endAfterStart"))
            .Must((dto, endsAt) => endsAt.ToUtc() - dto.StartsAt.ToUtc() <= EventLimits.MaxDuration)
            .WithMessage(_ => Loc.T("val.event.maxDays", EventLimits.MaxDuration.Days))
            .When(x => x.EndsAt.HasValue);

        RuleFor(x => x.Capacity!.Value)
            .InclusiveBetween(1, EventLimits.MaxCapacity).WithMessage(_ => Loc.T("val.event.capacity", EventLimits.MaxCapacity))
            .When(x => x.Capacity.HasValue);

        RuleFor(x => x.OnlineLink)
            .NotEmpty().WithMessage(_ => Loc.T("val.event.onlineLink"))
            .When(x => x.IsOnline);

        RuleFor(x => x.OnlineLink)
            .MaximumLength(500)
            .Must(IsHttpUrl).WithMessage(_ => Loc.T("val.event.linkHttp"))
            .When(x => x.IsOnline && !string.IsNullOrWhiteSpace(x.OnlineLink));
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
