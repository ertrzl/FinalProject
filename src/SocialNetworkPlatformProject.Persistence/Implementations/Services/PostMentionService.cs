using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PostMentionService : IPostMentionService
{
    private const int BackfillBatchSize = 200;

    private readonly IPostMentionRepository _mentions;
    private readonly IPostRepository _posts;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly IPostAccessService _access;
    private readonly IPostDtoBuilder _dtos;

    public PostMentionService(
        IPostMentionRepository mentions,
        IPostRepository posts,
        IUserRepository users,
        IFriendService friends,
        IBlockService blocks,
        IPostAccessService access,
        IPostDtoBuilder dtos)
    {
        _mentions = mentions;
        _posts = posts;
        _users = users;
        _friends = friends;
        _blocks = blocks;
        _access = access;
        _dtos = dtos;
    }

    public async Task SyncAsync(Post post, string? text)
    {
        var names = MentionParser.ExtractUserNames(text);
        var users = names.Count == 0 ? new Dictionary<string, UserSummary>() : await _users.GetSummariesByUserNamesAsync(names);

        var wanted = users.Values.Select(u => u.Id).Where(id => id != post.AuthorId).ToHashSet();
        var stored = await _mentions.GetAll(m => m.PostId == post.Id).ToListAsync();

        var changed = false;
        foreach (var row in stored.Where(row => !wanted.Contains(row.MentionedUserId)))
        {
            _mentions.Delete(row);
            changed = true;
        }

        var already = stored.Select(row => row.MentionedUserId).ToHashSet();
        foreach (var userId in wanted.Where(id => !already.Contains(id)))
        {
            await _mentions.AddAsync(new PostMention { PostId = post.Id, MentionedUserId = userId });
            changed = true;
        }

        if (!changed)
            return;

        try
        {
            await _mentions.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
        }
    }

    public async Task<PagedResult<GetPostDto>> GetTaggedPostsAsync(Guid profileUserId, Guid viewerId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var empty = new PagedResult<GetPostDto> { Page = page, PageSize = pageSize };

        var owner = await _users.GetSummaryAsync(profileUserId)
            ?? throw new NotFoundException(Loc.T("user.notFound"));

        if (profileUserId != viewerId)
        {
            if (await _blocks.HasBlockedAsync(profileUserId, viewerId))
                throw new NotFoundException(Loc.T("user.notFound"));
            if (await _blocks.HasBlockedAsync(viewerId, profileUserId))
                return empty;
            if (owner.IsPrivateAccount && !await _friends.AreFriendsAsync(profileUserId, viewerId))
                return empty;
        }

        var tagged = _mentions.PostIdsMentioning(profileUserId);
        var posts = _posts.GetAll(p => tagged.Contains(p.Id), asNoTracking: true);

        var visible = _access.VisibleTo(posts, viewerId);
        if (profileUserId != viewerId)
            visible = _access.VisibleTo(visible, profileUserId);

        var total = await visible.CountAsync();
        var pagePosts = await visible
            .Include("Hashtags.Hashtag").Include("Media").Include("Poll.Options")
            .OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GetPostDto>
        {
            Items = await _dtos.BuildAsync(pagePosts, viewerId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<int> BackfillAsync()
    {
        if (await _mentions.AnyAsync(_ => true))
            return 0;

        var stored = 0;
        for (var page = 1; ; page++)
        {
            var batch = await _posts.GetAll(p => p.Text != null && p.Text.Contains("@"), orderBy: p => p.Id, asNoTracking: true, page: page, take: BackfillBatchSize)
                .Select(p => new { p.Id, p.AuthorId, p.Text })
                .ToListAsync();
            if (batch.Count == 0)
                break;

            var users = await _users.GetSummariesByUserNamesAsync(batch.SelectMany(p => MentionParser.ExtractUserNames(p.Text)).Distinct().ToList());
            foreach (var post in batch)
            {
                var ids = MentionParser.ExtractUserNames(post.Text)
                    .Where(users.ContainsKey)
                    .Select(name => users[name].Id)
                    .Where(id => id != post.AuthorId)
                    .Distinct();

                foreach (var userId in ids)
                {
                    await _mentions.AddAsync(new PostMention { PostId = post.Id, MentionedUserId = userId });
                    stored++;
                }
            }

            await _mentions.SaveChangesAsync();
            if (batch.Count < BackfillBatchSize)
                break;
        }

        return stored;
    }
}
