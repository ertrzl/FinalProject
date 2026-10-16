using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

// What a group list card needs about the members, counted by the database: how many there are and what role the viewer has
// in the group (null when the viewer is not a member). The member rows themselves are not loaded.
public record GroupMembership(int MemberCount, GroupMemberRole? ViewerRole);
