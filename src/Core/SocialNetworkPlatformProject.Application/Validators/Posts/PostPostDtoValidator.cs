using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PostPostDtoValidator : AbstractValidator<PostPostDto>
{
    public PostPostDtoValidator()
    {
        // Matches publishPost()'s own check in feed.js/profile.js/group.js: a post needs text OR media, not necessarily both.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.Media.Count > 0)
            .WithMessage("A post must have either text or media.");

        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage("Post text cannot exceed 5000 characters.");

        // Photos together (up to the limit) or a single video; mixing the two would make the card ambiguous.
        RuleFor(x => x.Media)
            .Must(files => files.Count <= PostLimits.MaxImages)
            .WithMessage($"A post can have at most {PostLimits.MaxImages} photos.");

        RuleFor(x => x.Media)
            .Must(files => !files.Any(IsVideo) || files.Count == 1)
            .WithMessage("A post can have either photos or a single video, not both.");

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage("Privacy must be either 'Public' or 'FriendsOnly'.");

        When(x => x.PollOptions.Count > 0, () =>
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("A poll needs a question: write it as the text of the post.");

            RuleFor(x => x.Media)
                .Must(files => files.Count == 0).WithMessage("A poll cannot have photos or a video.");

            RuleFor(x => x.PollOptions)
                .Must(options => options.Count >= PollLimits.MinOptions && options.Count <= PollLimits.MaxOptions)
                .WithMessage($"A poll needs between {PollLimits.MinOptions} and {PollLimits.MaxOptions} answers.");

            RuleFor(x => x.PollOptions)
                .Must(options => options.All(o => !string.IsNullOrWhiteSpace(o) && o.Trim().Length <= PollLimits.MaxOptionLength))
                .WithMessage($"Every answer must be filled in and at most {PollLimits.MaxOptionLength} characters long.");

            RuleFor(x => x.PollOptions)
                .Must(options => options.Select(o => (o ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == options.Count)
                .WithMessage("The answers of a poll must all be different.");

            RuleFor(x => x.PollDurationHours)
                .Must(hours => hours is null || (hours >= PollLimits.MinHours && hours <= PollLimits.MaxHours))
                .WithMessage($"A poll stays open between {PollLimits.MinHours} hour and {PollLimits.MaxHours / 24} days.");
        });
    }

    private static bool IsVideo(Microsoft.AspNetCore.Http.IFormFile file) =>
        file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
}
