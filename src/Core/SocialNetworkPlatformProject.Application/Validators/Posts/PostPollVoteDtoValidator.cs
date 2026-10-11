using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PostPollVoteDtoValidator : AbstractValidator<PostPollVoteDto>
{
    public PostPollVoteDtoValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty().WithMessage("Choose an answer.");
    }
}
