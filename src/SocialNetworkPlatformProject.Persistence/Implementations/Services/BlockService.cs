using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class BlockService : IBlockService
{
    private readonly IUserBlockRepository _blocks;
    private readonly IUserRepository _users;
    private readonly IFriendshipRepository _friendships;
    private readonly IFriendRequestRepository _requests;
    private readonly INotificationRepository _notifications;

    public BlockService(
        IUserBlockRepository blocks,
        IUserRepository users,
        IFriendshipRepository friendships,
        IFriendRequestRepository requests,
        INotificationRepository notifications)
    {
        _blocks = blocks;
        _users = users;
        _friendships = friendships;
        _requests = requests;
        _notifications = notifications;
    }

    public async Task BlockAsync(Guid currentUserId, Guid userId)
    {
        if (userId == currentUserId)
            throw new BadRequestException("You can't block yourself.");

        if (!await _users.ExistsAsync(userId))
            throw new NotFoundException("User not found.");

        if (await HasBlockedAsync(currentUserId, userId))
            throw new ConflictException("You already blocked this user.");

        await _blocks.AddAsync(new UserBlock { BlockerId = currentUserId, BlockedId = userId });

        // From now on the two are strangers to each other: no friendship, no open requests, no old notifications.
        var friendships = await _friendships.GetAll(f =>
                (f.UserOneId == currentUserId && f.UserTwoId == userId) ||
                (f.UserOneId == userId && f.UserTwoId == currentUserId))
            .ToListAsync();
        foreach (var friendship in friendships)
            _friendships.Delete(friendship);

        var pending = await _requests.GetAll(r =>
                r.Status == FriendRequestStatus.Pending &&
                ((r.SenderId == currentUserId && r.ReceiverId == userId) || (r.SenderId == userId && r.ReceiverId == currentUserId)))
            .ToListAsync();
        foreach (var request in pending)
        {
            request.Status = FriendRequestStatus.Cancelled;
            request.RespondedAt = DateTime.UtcNow;
        }

        var notifications = await _notifications.GetAll(n =>
                (n.RecipientId == currentUserId && n.ActorId == userId) || (n.RecipientId == userId && n.ActorId == currentUserId))
            .ToListAsync();
        foreach (var notification in notifications)
            _notifications.Delete(notification);

        try
        {
            await _blocks.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // A second tab blocked the same person a moment earlier.
            throw new ConflictException("You already blocked this user.");
        }
    }

    public async Task UnblockAsync(Guid currentUserId, Guid userId)
    {
        var block = await _blocks.GetAll(b => b.BlockerId == currentUserId && b.BlockedId == userId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("You have not blocked this user.");

        _blocks.Delete(block);
        await _blocks.SaveChangesAsync();
    }

    public async Task<List<GetBlockedUserDto>> GetBlockedAsync(Guid currentUserId)
    {
        var blocks = await _blocks.GetAll(
                filter: b => b.BlockerId == currentUserId,
                orderBy: b => b.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        var users = await _users.GetSummariesAsync(blocks.Select(b => b.BlockedId));

        return blocks
            .Where(b => users.ContainsKey(b.BlockedId))
            .Select(b => new GetBlockedUserDto
            {
                UserId = b.BlockedId,
                Name = users[b.BlockedId].FullName,
                UserName = users[b.BlockedId].UserName,
                AvatarUrl = users[b.BlockedId].AvatarUrl,
                BlockedAt = b.CreatedAt
            })
            .ToList();
    }

    public async Task<bool> AreBlockedAsync(Guid userA, Guid userB)
    {
        return await _blocks.AnyAsync(b =>
            (b.BlockerId == userA && b.BlockedId == userB) || (b.BlockerId == userB && b.BlockedId == userA));
    }

    public async Task<bool> HasBlockedAsync(Guid blockerId, Guid blockedId)
    {
        return await _blocks.AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
    }

    public async Task<HashSet<Guid>> GetBlockedIdsAsync(Guid userId)
    {
        var ids = await _blocks.GetAll(b => b.BlockerId == userId || b.BlockedId == userId, asNoTracking: true)
            .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
            .ToListAsync();

        return ids.ToHashSet();
    }
}
