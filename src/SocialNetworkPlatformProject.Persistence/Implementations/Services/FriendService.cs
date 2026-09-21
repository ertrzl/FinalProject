using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Friends;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class FriendService : IFriendService
{
    private readonly IFriendRequestRepository _requests;
    private readonly IFriendshipRepository _friendships;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IMapper _mapper;

    public FriendService(
        IFriendRequestRepository requests,
        IFriendshipRepository friendships,
        IUserRepository users,
        INotificationService notifications,
        IMapper mapper)
    {
        _requests = requests;
        _friendships = friendships;
        _users = users;
        _notifications = notifications;
        _mapper = mapper;
    }

    public async Task<GetFriendRequestDto> SendRequestAsync(Guid currentUserId, PostFriendRequestDto dto)
    {
        if (dto.ReceiverId == currentUserId)
            throw new BadRequestException("You can't send a friend request to yourself.");

        if (!await _users.ExistsAsync(dto.ReceiverId))
            throw new NotFoundException("User not found.");

        if (await AreFriendsAsync(currentUserId, dto.ReceiverId))
            throw new ConflictException("You are already friends.");

        var alreadySent = await _requests.AnyAsync(r =>
            r.Status == FriendRequestStatus.Pending && r.SenderId == currentUserId && r.ReceiverId == dto.ReceiverId);
        if (alreadySent)
            throw new ConflictException("You already sent a friend request to this user.");

        var alreadyReceived = await _requests.AnyAsync(r =>
            r.Status == FriendRequestStatus.Pending && r.SenderId == dto.ReceiverId && r.ReceiverId == currentUserId);
        if (alreadyReceived)
            throw new ConflictException("This user already sent you a request — accept it instead.");

        var request = new FriendRequest { SenderId = currentUserId, ReceiverId = dto.ReceiverId };
        await _requests.AddAsync(request);
        await _requests.SaveChangesAsync();

        await _notifications.CreateAsync(dto.ReceiverId, currentUserId, NotificationType.FriendRequestReceived,
            friendRequestId: request.Id);

        return (await BuildRequestDtosAsync(new[] { request }, currentUserId))[0];
    }

    public async Task AcceptRequestAsync(Guid currentUserId, Guid requestId)
    {
        var request = await GetPendingRequestAsync(requestId);
        if (request.ReceiverId != currentUserId)
            throw new ForbiddenException("Only the receiver can accept this request.");

        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;

        // Store each pair in one canonical order so the unique index also blocks the reversed duplicate.
        var (first, second) = request.SenderId.CompareTo(request.ReceiverId) < 0
            ? (request.SenderId, request.ReceiverId)
            : (request.ReceiverId, request.SenderId);

        if (!await _friendships.AnyAsync(f => f.UserOneId == first && f.UserTwoId == second))
            await _friendships.AddAsync(new Friendship { UserOneId = first, UserTwoId = second });

        await _requests.SaveChangesAsync();

        await _notifications.CreateAsync(request.SenderId, currentUserId, NotificationType.FriendRequestAccepted,
            friendRequestId: request.Id);
    }

    public async Task DeclineRequestAsync(Guid currentUserId, Guid requestId)
    {
        var request = await GetPendingRequestAsync(requestId);
        if (request.ReceiverId != currentUserId)
            throw new ForbiddenException("Only the receiver can decline this request.");

        request.Status = FriendRequestStatus.Declined;
        request.RespondedAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync();
    }

    public async Task CancelRequestAsync(Guid currentUserId, Guid requestId)
    {
        var request = await GetPendingRequestAsync(requestId);
        if (request.SenderId != currentUserId)
            throw new ForbiddenException("Only the sender can cancel this request.");

        request.Status = FriendRequestStatus.Cancelled;
        request.RespondedAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync();
    }

    public async Task<List<GetFriendRequestDto>> GetIncomingRequestsAsync(Guid currentUserId)
    {
        var requests = await _requests.GetAll(
                filter: r => r.ReceiverId == currentUserId && r.Status == FriendRequestStatus.Pending,
                orderBy: r => r.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        return await BuildRequestDtosAsync(requests, currentUserId);
    }

    public async Task<List<GetFriendRequestDto>> GetSentRequestsAsync(Guid currentUserId)
    {
        var requests = await _requests.GetAll(
                filter: r => r.SenderId == currentUserId && r.Status == FriendRequestStatus.Pending,
                orderBy: r => r.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        return await BuildRequestDtosAsync(requests, currentUserId);
    }

    public async Task<List<GetFriendDto>> GetFriendsAsync(Guid userId)
    {
        var friendships = await _friendships.GetAll(
                filter: f => f.UserOneId == userId || f.UserTwoId == userId,
                orderBy: f => f.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        var friendIds = friendships.Select(f => f.UserOneId == userId ? f.UserTwoId : f.UserOneId).ToList();
        var summaries = await _users.GetSummariesAsync(friendIds);

        var result = new List<GetFriendDto>();
        foreach (var friendship in friendships)
        {
            var friendId = friendship.UserOneId == userId ? friendship.UserTwoId : friendship.UserOneId;
            if (!summaries.TryGetValue(friendId, out var friend))
                continue;

            result.Add(new GetFriendDto
            {
                UserId = friend.Id,
                Name = friend.FullName,
                AvatarUrl = friend.AvatarUrl,
                Location = friend.Location,
                FriendsSince = friendship.CreatedAt
            });
        }

        return result;
    }

    public async Task RemoveFriendAsync(Guid currentUserId, Guid friendUserId)
    {
        var friendship = await _friendships.GetAll(f =>
                (f.UserOneId == currentUserId && f.UserTwoId == friendUserId) ||
                (f.UserOneId == friendUserId && f.UserTwoId == currentUserId))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("You are not friends with this user.");

        _friendships.Delete(friendship);
        await _friendships.SaveChangesAsync();
    }

    public async Task<List<GetUserSearchResultDto>> GetSuggestionsAsync(Guid currentUserId, int take)
    {
        take = Math.Clamp(take, 1, 50);

        var myFriendIds = await GetFriendIdsAsync(currentUserId);

        var pendingCounterparts = await _requests.GetAll(
                r => r.Status == FriendRequestStatus.Pending && (r.SenderId == currentUserId || r.ReceiverId == currentUserId),
                asNoTracking: true)
            .Select(r => r.SenderId == currentUserId ? r.ReceiverId : r.SenderId)
            .ToListAsync();

        var excluded = new HashSet<Guid>(myFriendIds.Concat(pendingCounterparts)) { currentUserId };

        // Friends of friends, ranked by how many friends they share with the current user.
        var friendsOfFriends = await _friendships.GetAll(
                f => myFriendIds.Contains(f.UserOneId) || myFriendIds.Contains(f.UserTwoId),
                asNoTracking: true)
            .ToListAsync();

        var mutualCounts = new Dictionary<Guid, int>();
        foreach (var friendship in friendsOfFriends)
        {
            var candidate = myFriendIds.Contains(friendship.UserOneId) ? friendship.UserTwoId : friendship.UserOneId;
            if (excluded.Contains(candidate))
                continue;

            mutualCounts[candidate] = mutualCounts.GetValueOrDefault(candidate) + 1;
        }

        var rankedIds = mutualCounts.OrderByDescending(kv => kv.Value).Take(take).Select(kv => kv.Key).ToList();
        var summaries = (await _users.GetSummariesAsync(rankedIds)).Values.ToList();

        // Pad with the newest users so a brand-new account still sees people to add.
        if (summaries.Count < take)
        {
            var alreadyPicked = excluded.Concat(summaries.Select(s => s.Id));
            summaries.AddRange(await _users.GetRecentAsync(alreadyPicked, take - summaries.Count));
        }

        return summaries
            .OrderByDescending(s => mutualCounts.GetValueOrDefault(s.Id))
            .Select(s => new GetUserSearchResultDto
            {
                Id = s.Id,
                FullName = s.FullName,
                UserName = s.UserName,
                AvatarUrl = s.AvatarUrl,
                MutualFriendsCount = mutualCounts.GetValueOrDefault(s.Id),
                FriendshipStatus = "None"
            })
            .ToList();
    }

    public async Task<bool> AreFriendsAsync(Guid userA, Guid userB)
    {
        return await _friendships.AnyAsync(f =>
            (f.UserOneId == userA && f.UserTwoId == userB) ||
            (f.UserOneId == userB && f.UserTwoId == userA));
    }

    public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
    {
        return await _friendships.GetAll(f => f.UserOneId == userId || f.UserTwoId == userId, asNoTracking: true)
            .Select(f => f.UserOneId == userId ? f.UserTwoId : f.UserOneId)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, string>> GetFriendshipStatusesAsync(Guid currentUserId, IEnumerable<Guid> otherUserIds)
    {
        var ids = otherUserIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => "None");
        if (ids.Count == 0)
            return result;

        var friendIds = (await GetFriendIdsAsync(currentUserId)).ToHashSet();

        var pending = await _requests.GetAll(
                r => r.Status == FriendRequestStatus.Pending && (r.SenderId == currentUserId || r.ReceiverId == currentUserId),
                asNoTracking: true)
            .ToListAsync();

        foreach (var id in ids)
        {
            if (friendIds.Contains(id))
                result[id] = "Friends";
            else if (pending.Any(r => r.SenderId == currentUserId && r.ReceiverId == id))
                result[id] = "RequestSent";
            else if (pending.Any(r => r.SenderId == id && r.ReceiverId == currentUserId))
                result[id] = "RequestReceived";
        }

        return result;
    }

    public async Task<Dictionary<Guid, int>> GetMutualFriendCountsAsync(Guid currentUserId, IEnumerable<Guid> otherUserIds)
    {
        var ids = otherUserIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => 0);
        if (ids.Count == 0)
            return result;

        var myFriendIds = (await GetFriendIdsAsync(currentUserId)).ToHashSet();
        if (myFriendIds.Count == 0)
            return result;

        var theirFriendships = await _friendships.GetAll(
                f => ids.Contains(f.UserOneId) || ids.Contains(f.UserTwoId),
                asNoTracking: true)
            .ToListAsync();

        foreach (var friendship in theirFriendships)
        {
            if (result.ContainsKey(friendship.UserOneId) && myFriendIds.Contains(friendship.UserTwoId))
                result[friendship.UserOneId]++;

            if (result.ContainsKey(friendship.UserTwoId) && myFriendIds.Contains(friendship.UserOneId))
                result[friendship.UserTwoId]++;
        }

        return result;
    }

    private async Task<FriendRequest> GetPendingRequestAsync(Guid requestId)
    {
        var request = await _requests.GetByIdAsync(requestId)
            ?? throw new NotFoundException("Friend request not found.");

        if (request.Status != FriendRequestStatus.Pending)
            throw new BadRequestException("This friend request has already been answered.");

        return request;
    }

    private async Task<List<GetFriendRequestDto>> BuildRequestDtosAsync(IEnumerable<FriendRequest> requests, Guid currentUserId)
    {
        var list = requests.ToList();
        var users = await _users.GetSummariesAsync(list.SelectMany(r => new[] { r.SenderId, r.ReceiverId }));

        var otherParties = list.Select(r => r.SenderId == currentUserId ? r.ReceiverId : r.SenderId);
        var mutualCounts = await GetMutualFriendCountsAsync(currentUserId, otherParties);

        var dtos = _mapper.Map<List<GetFriendRequestDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            var request = list[i];

            if (users.TryGetValue(request.SenderId, out var sender))
            {
                dtos[i].SenderName = sender.FullName;
                dtos[i].SenderAvatarUrl = sender.AvatarUrl;
            }

            if (users.TryGetValue(request.ReceiverId, out var receiver))
            {
                dtos[i].ReceiverName = receiver.FullName;
                dtos[i].ReceiverAvatarUrl = receiver.AvatarUrl;
            }

            var otherId = request.SenderId == currentUserId ? request.ReceiverId : request.SenderId;
            dtos[i].MutualFriendsCount = mutualCounts.GetValueOrDefault(otherId);
        }

        return dtos;
    }
}
