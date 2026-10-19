using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MutualConnectionService : IMutualConnectionService
{
    // How many of each are listed (the counts are always the real totals).
    private const int MaxListed = 100;

    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly IUserRepository _users;
    private readonly IGroupMemberRepository _members;
    private readonly IGroupRepository _groups;

    public MutualConnectionService(
        IFriendService friends,
        IBlockService blocks,
        IUserRepository users,
        IGroupMemberRepository members,
        IGroupRepository groups)
    {
        _friends = friends;
        _blocks = blocks;
        _users = users;
        _members = members;
        _groups = groups;
    }

    public async Task<GetMutualConnectionsDto> GetAsync(Guid viewerId, Guid userId)
    {
        var result = new GetMutualConnectionsDto();
        if (viewerId == userId)
            return result;

        if (!await _users.ExistsAsync(userId))
            throw new NotFoundException(Loc.T("user.notFound"));

        // Somebody who blocked the viewer looks as if they do not exist; somebody the viewer blocked has nothing shown.
        if (await _blocks.HasBlockedAsync(userId, viewerId))
            throw new NotFoundException(Loc.T("user.notFound"));
        if (await _blocks.HasBlockedAsync(viewerId, userId))
            return result;

        await AddFriendsAsync(result, viewerId, userId);
        await AddGroupsAsync(result, viewerId, userId);
        return result;
    }

    // People who are a friend of both, without anybody blocked either way with the viewer; listed by name.
    private async Task AddFriendsAsync(GetMutualConnectionsDto result, Guid viewerId, Guid userId)
    {
        var hidden = await _blocks.GetBlockedIdsAsync(viewerId);
        var theirs = (await _friends.GetFriendIdsAsync(userId)).ToHashSet();
        var common = (await _friends.GetFriendIdsAsync(viewerId)).Where(id => theirs.Contains(id) && !hidden.Contains(id)).ToList();
        result.FriendCount = common.Count;
        if (common.Count == 0)
            return;

        var summaries = await _users.GetSummariesAsync(common);
        result.Friends = summaries.Values
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Take(MaxListed)
            .Select(u => new GetMutualFriendDto { UserId = u.Id, FullName = u.FullName, UserName = u.UserName, AvatarUrl = u.AvatarUrl })
            .ToList();
    }

    // Groups both people belong to. The viewer is a member of every one of them, so a private group is only ever shown to a member
    // (and the other person's membership is no secret from the people in the same group).
    private async Task AddGroupsAsync(GetMutualConnectionsDto result, Guid viewerId, Guid userId)
    {
        var mine = await _members.GetAll(m => m.UserId == viewerId, asNoTracking: true).Select(m => m.GroupId).ToListAsync();
        if (mine.Count == 0)
            return;

        var commonIds = await _members.GetAll(m => m.UserId == userId && mine.Contains(m.GroupId), asNoTracking: true)
            .Select(m => m.GroupId)
            .ToListAsync();
        result.GroupCount = commonIds.Count;
        if (commonIds.Count == 0)
            return;

        var groups = await _groups.GetAll(g => commonIds.Contains(g.Id), orderBy: g => g.Name, asNoTracking: true).Take(MaxListed).ToListAsync();
        var memberships = await _groups.GetMembershipsAsync(groups.Select(g => g.Id).ToList(), viewerId);

        result.Groups = groups.Select(g => new GetMutualGroupDto
        {
            Id = g.Id,
            Name = g.Name,
            CoverImageUrl = g.CoverImageUrl,
            MemberCount = memberships.GetValueOrDefault(g.Id)?.MemberCount ?? 0
        }).ToList();
    }
}
