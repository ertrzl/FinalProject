namespace SocialNetworkPlatformProject.Application.Common;

// The two numbers a post card shows, counted by the database instead of loading every like and comment row.
public record PostCounts(int Likes, int Comments);
