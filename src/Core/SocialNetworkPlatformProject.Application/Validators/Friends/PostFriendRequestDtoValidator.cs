using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Friends;

namespace SocialNetworkPlatformProject.Application.Validators.Friends;

public class PostFriendRequestDtoValidator : AbstractValidator<PostFriendRequestDto>
{
    public PostFriendRequestDtoValidator()
    {
        RuleFor(x => x.ReceiverId).NotEmpty();
    }
}
