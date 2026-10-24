using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

public record GroupMembership(int MemberCount, GroupMemberRole? ViewerRole);
