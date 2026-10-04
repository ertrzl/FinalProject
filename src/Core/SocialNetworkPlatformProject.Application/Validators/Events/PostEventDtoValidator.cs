using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventDtoValidator : EventInputValidator<PostEventDto>
{
    public PostEventDtoValidator()
    {
        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt.ToUtc() > DateTime.UtcNow).WithMessage("Etkinlik başlangıcı gelecekte olmalı.");

        RuleFor(x => x.IsPrivate)
            .Equal(false).When(x => x.GroupId.HasValue)
            .WithMessage("Grup etkinlikleri zaten sadece grup üyelerine açıktır, ayrıca özel yapılamaz.");
    }
}
