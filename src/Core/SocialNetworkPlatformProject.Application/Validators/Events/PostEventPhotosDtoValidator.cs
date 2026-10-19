using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventPhotosDtoValidator : AbstractValidator<PostEventPhotosDto>
{
    public PostEventPhotosDtoValidator()
    {
        RuleFor(x => x.Photos)
            .Must(files => files.Count > 0).WithMessage(_ => Loc.T("val.eventPhoto.none"))
            .Must(files => files.Count <= EventLimits.MaxPhotosPerPerson)
            .WithMessage(_ => Loc.T("val.eventPhoto.perRequest", EventLimits.MaxPhotosPerPerson));
    }
}
