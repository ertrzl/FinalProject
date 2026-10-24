namespace SocialNetworkPlatformProject.Application.Common;

public static class PollLimits
{
    public const int MinOptions = 2;
    public const int MaxOptions = 6;
    public const int MaxOptionLength = 80;

    public const int MinHours = 1;
    public const int MaxHours = 24 * 7;
    public const int DefaultHours = 24;
}
