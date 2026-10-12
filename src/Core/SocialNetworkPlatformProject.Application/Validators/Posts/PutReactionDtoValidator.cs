using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PutReactionDtoValidator : AbstractValidator<PutReactionDto>
{
    public PutReactionDtoValidator()
    {
        // Empty = take the reaction back; anything else must be one of the six reactions (exact spelling).
        RuleFor(x => x.Type)
            .Must(type => string.IsNullOrEmpty(type) || Enum.GetNames<ReactionType>().Contains(type))
            .WithMessage(_ => Loc.T("val.reaction.type", string.Join(", ", Enum.GetNames<ReactionType>())));
    }
}
