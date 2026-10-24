using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PostPostDtoValidator : AbstractValidator<PostPostDto>
{
    public PostPostDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.Media.Count > 0)
            .WithMessage(_ => Loc.T("post.needContent"));

        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage(_ => Loc.T("val.post.long"));

        RuleFor(x => x.Media)
            .Must(files => files.Count <= PostLimits.MaxImages)
            .WithMessage(_ => Loc.T("val.post.photos", PostLimits.MaxImages));

        RuleFor(x => x.Media)
            .Must(files => !files.Any(IsVideo) || files.Count == 1)
            .WithMessage(_ => Loc.T("val.post.mediaMix"));

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage(_ => Loc.T("post.privacy"));

        When(x => x.PollOptions.Count > 0, () =>
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(_ => Loc.T("val.poll.question"));

            RuleFor(x => x.Media)
                .Must(files => files.Count == 0).WithMessage(_ => Loc.T("val.poll.noMedia"));

            RuleFor(x => x.PollOptions)
                .Must(options => options.Count >= PollLimits.MinOptions && options.Count <= PollLimits.MaxOptions)
                .WithMessage(_ => Loc.T("val.poll.options", PollLimits.MinOptions, PollLimits.MaxOptions));

            RuleFor(x => x.PollOptions)
                .Must(options => options.All(o => !string.IsNullOrWhiteSpace(o) && o.Trim().Length <= PollLimits.MaxOptionLength))
                .WithMessage(_ => Loc.T("val.poll.optionText", PollLimits.MaxOptionLength));

            RuleFor(x => x.PollOptions)
                .Must(options => options.Select(o => (o ?? string.Empty).Trim().ToLowerInvariant()).Distinct().Count() == options.Count)
                .WithMessage(_ => Loc.T("val.poll.distinct"));

            RuleFor(x => x.PollDurationHours)
                .Must(hours => hours is null || (hours >= PollLimits.MinHours && hours <= PollLimits.MaxHours))
                .WithMessage(_ => Loc.T("val.poll.hours", PollLimits.MinHours, PollLimits.MaxHours / 24));
        });
    }

    private static bool IsVideo(Microsoft.AspNetCore.Http.IFormFile file) =>
        file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
}
