using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

// Rules shared by creating and editing an event.
public abstract class EventInputValidator<T> : AbstractValidator<T> where T : EventInputDto
{
    protected EventInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MaximumLength(150);

        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);

        RuleFor(x => x.EndsAt!.Value)
            .Must((dto, endsAt) => endsAt.ToUtc() > dto.StartsAt.ToUtc()).WithMessage("Bitiş zamanı başlangıçtan sonra olmalı.")
            .Must((dto, endsAt) => endsAt.ToUtc() - dto.StartsAt.ToUtc() <= EventLimits.MaxDuration)
            .WithMessage($"Etkinlik en fazla {EventLimits.MaxDuration.Days} gün sürebilir.")
            .When(x => x.EndsAt.HasValue);

        RuleFor(x => x.Capacity!.Value)
            .InclusiveBetween(1, EventLimits.MaxCapacity).WithMessage($"Kontenjan 1 ile {EventLimits.MaxCapacity} arasında olmalı.")
            .When(x => x.Capacity.HasValue);

        RuleFor(x => x.OnlineLink)
            .NotEmpty().WithMessage("Çevrimiçi etkinlik için bir bağlantı gerekli.")
            .When(x => x.IsOnline);

        RuleFor(x => x.OnlineLink)
            .MaximumLength(500)
            .Must(IsHttpUrl).WithMessage("Bağlantı http:// veya https:// ile başlayan geçerli bir adres olmalı.")
            .When(x => x.IsOnline && !string.IsNullOrWhiteSpace(x.OnlineLink));
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
