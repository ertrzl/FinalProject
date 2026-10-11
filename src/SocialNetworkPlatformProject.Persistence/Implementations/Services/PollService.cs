using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PollService : IPollService
{
    private static readonly string[] PostIncludes = { "Hashtags.Hashtag", "Media", "Poll.Options" };

    private readonly IPostRepository _posts;
    private readonly IPollVoteRepository _votes;
    private readonly IPostAccessService _access;
    private readonly IPostDtoBuilder _dtos;
    private readonly ILiveUpdateService _live;

    public PollService(
        IPostRepository posts,
        IPollVoteRepository votes,
        IPostAccessService access,
        IPostDtoBuilder dtos,
        ILiveUpdateService live)
    {
        _posts = posts;
        _votes = votes;
        _access = access;
        _dtos = dtos;
        _live = live;
    }

    public async Task<GetPostDto> VoteAsync(Guid currentUserId, Guid postId, Guid optionId)
    {
        var post = await _posts.GetByIdAsync(postId, PostIncludes)
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        var poll = post.Poll ?? throw new BadRequestException("This post has no poll.");

        if (poll.IsEnded(DateTime.UtcNow))
            throw new BadRequestException("This poll has ended.");

        if (poll.Options.All(o => o.Id != optionId))
            throw new BadRequestException("That answer does not belong to this poll.");

        if (await _votes.AnyAsync(v => v.PollId == poll.Id && v.UserId == currentUserId))
            throw new ConflictException("You have already voted in this poll.");

        await _votes.AddAsync(new PollVote { PollId = poll.Id, OptionId = optionId, UserId = currentUserId });
        try
        {
            await _votes.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two requests of the same person at the same moment: the other one counted.
            throw new ConflictException("You have already voted in this poll.");
        }

        await _live.PollVotedAsync(post, currentUserId);
        return (await _dtos.BuildAsync(new[] { post }, currentUserId))[0];
    }
}
