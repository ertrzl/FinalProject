using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class GroupPostService : IGroupPostService
{
    private static readonly string[] PostIncludes = { "Hashtags.Hashtag", "Media", "Poll.Options" };

    private readonly IGroupService _groups;
    private readonly IGroupRepository _groupRows;
    private readonly IGroupPinnedPostRepository _pins;
    private readonly IPostRepository _posts;
    private readonly IPostAccessService _access;
    private readonly IPostDtoBuilder _dtos;
    private readonly INotificationService _notifications;

    public GroupPostService(
        IGroupService groups,
        IGroupRepository groupRows,
        IGroupPinnedPostRepository pins,
        IPostRepository posts,
        IPostAccessService access,
        IPostDtoBuilder dtos,
        INotificationService notifications)
    {
        _groups = groups;
        _groupRows = groupRows;
        _pins = pins;
        _posts = posts;
        _access = access;
        _dtos = dtos;
        _notifications = notifications;
    }

    public async Task<List<GetPostDto>> GetPinnedAsync(Guid currentUserId, Guid groupId)
    {
        await _groups.GetByIdAsync(currentUserId, groupId);

        return await PinnedForAsync(currentUserId, groupId);
    }

    public async Task<List<GetPostDto>> PinAsync(Guid currentUserId, Guid groupId, Guid postId)
    {
        var group = await GetAsAdminAsync(currentUserId, groupId);

        var post = await _posts.GetByIdAsync(postId);
        if (post == null || post.GroupId != groupId)
            throw new NotFoundException(Loc.T("post.notFound"));

        var pinnedIds = await _pins.GetPinnedPostIdsAsync(groupId);
        if (pinnedIds.Contains(postId))
            return await PinnedForAsync(currentUserId, groupId);

        if (pinnedIds.Count >= GroupLimits.MaxPinnedPosts)
            throw new BadRequestException(Loc.T("group.pinLimit", GroupLimits.MaxPinnedPosts));

        await _pins.AddAsync(new GroupPinnedPost { GroupId = groupId, PostId = postId });
        try
        {
            await _pins.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return await PinnedForAsync(currentUserId, groupId);
        }

        foreach (var member in group.Members.Where(m => m.UserId != currentUserId))
            await _notifications.CreateAsync(member.UserId, currentUserId, NotificationType.GroupPostPinned, postId: postId, groupId: groupId);

        return await PinnedForAsync(currentUserId, groupId);
    }

    public async Task<List<GetPostDto>> UnpinAsync(Guid currentUserId, Guid groupId, Guid postId)
    {
        await GetAsAdminAsync(currentUserId, groupId);

        var pin = await _pins.GetAll(p => p.GroupId == groupId && p.PostId == postId).FirstOrDefaultAsync();
        if (pin != null)
        {
            _pins.Delete(pin);
            await _pins.SaveChangesAsync();
        }

        return await PinnedForAsync(currentUserId, groupId);
    }

    public async Task<PagedResult<GetPostDto>> SearchAsync(Guid currentUserId, Guid groupId, string? term, int page, int pageSize)
    {
        await _groups.GetByIdAsync(currentUserId, groupId);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        term = term?.Trim() ?? string.Empty;

        if (term.Length == 0)
            return new PagedResult<GetPostDto> { Page = page, PageSize = pageSize };

        Expression<Func<Post, bool>> filter;
        if (term.StartsWith('#') && term.Length > 1)
        {
            var tag = term[1..].ToLowerInvariant();
            filter = p => p.GroupId == groupId && p.Hashtags.Any(h => h.Hashtag!.Name == tag);
        }
        else
        {
            var tag = term.ToLowerInvariant();
            filter = p => p.GroupId == groupId
                && ((p.Text != null && p.Text.Contains(term)) || p.Hashtags.Any(h => h.Hashtag!.Name == tag));
        }

        var visible = _access.VisibleTo(_posts.GetAll(filter, asNoTracking: true), currentUserId);
        var total = await visible.CountAsync();
        var pagePosts = await visible
            .Include("Hashtags.Hashtag").Include("Media").Include("Poll.Options")
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GetPostDto>
        {
            Items = await _dtos.BuildAsync(pagePosts, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private async Task<List<GetPostDto>> PinnedForAsync(Guid currentUserId, Guid groupId)
    {
        var pinnedIds = await _pins.GetPinnedPostIdsAsync(groupId);
        if (pinnedIds.Count == 0)
            return new List<GetPostDto>();

        var posts = await _access.VisibleTo(
                _posts.GetAll(p => pinnedIds.Contains(p.Id) && p.GroupId == groupId, asNoTracking: true, includes: PostIncludes),
                currentUserId)
            .ToListAsync();

        var ordered = posts.OrderBy(p => pinnedIds.IndexOf(p.Id)).ToList();
        return await _dtos.BuildAsync(ordered, currentUserId);
    }

    private async Task<Group> GetAsAdminAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groupRows.GetByIdAsync(groupId, "Members")
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        if (!group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin))
            throw new ForbiddenException(Loc.T("group.adminOnly"));

        return group;
    }
}
