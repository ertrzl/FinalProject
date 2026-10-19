using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Highlights;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class PostHighlightDtoValidator : AbstractValidator<PostHighlightDto>
{
    public PostHighlightDtoValidator()
    {
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= HighlightLimits.MaxTitleLength)
            .WithMessage(_ => Loc.T("val.highlight.title", HighlightLimits.MaxTitleLength));

        RuleFor(x => x.StoryId).NotEmpty().WithMessage(_ => Loc.T("story.notFound"));
    }
}

public class PutHighlightDtoValidator : AbstractValidator<PutHighlightDto>
{
    public PutHighlightDtoValidator()
    {
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= HighlightLimits.MaxTitleLength)
            .WithMessage(_ => Loc.T("val.highlight.title", HighlightLimits.MaxTitleLength));
    }
}
