using SocialNetworkPlatformProject.Application.DTOs.Friends;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IFriendService
{
    Task<GetFriendRequestDto> SendRequestAsync(Guid currentUserId, PostFriendRequestDto dto);

    Task AcceptRequestAsync(Guid currentUserId, Guid requestId);

    Task DeclineRequestAsync(Guid currentUserId, Guid requestId);

    // The sender withdraws their own pending request.
    Task CancelRequestAsync(Guid currentUserId, Guid requestId);

    Task<List<GetFriendRequestDto>> GetIncomingRequestsAsync(Guid currentUserId);

    Task<List<GetFriendRequestDto>> GetSentRequestsAsync(Guid currentUserId);

    Task<List<GetFriendDto>> GetFriendsAsync(Guid userId);

    // Somebody's friend list as the viewer may see it: always your own; another person's only when they switched
    // "show my friends" on (otherwise Forbidden). Somebody who blocked you looks as if they do not exist, and people
    // you blocked or who blocked you are left out of the list.
    Task<List<GetFriendDto>> GetFriendListAsync(Guid viewerId, Guid userId);

    Task RemoveFriendAsync(Guid currentUserId, Guid friendUserId);

    Task<List<GetUserSearchResultDto>> GetSuggestionsAsync(Guid currentUserId, int take);

    // Helpers other services build on (feeds, stories, profile/search DTOs).
    Task<bool> AreFriendsAsync(Guid userA, Guid userB);

    Task<List<Guid>> GetFriendIdsAsync(Guid userId);

    // "None" / "Friends" / "RequestSent" / "RequestReceived" for each id.
    Task<Dictionary<Guid, string>> GetFriendshipStatusesAsync(Guid currentUserId, IEnumerable<Guid> otherUserIds);

    Task<Dictionary<Guid, int>> GetMutualFriendCountsAsync(Guid currentUserId, IEnumerable<Guid> otherUserIds);
}
