namespace SocialNetworkPlatformProject.Application.Common;

// The longest texts people can write, as the database columns hold them (the validators of the DTOs refuse longer
// ones first, with a proper message). Without a limit on the column a text that got past the validators, or went around
// them, would be stored whatever its size. Keep these in step with the validators.
public static class TextLimits
{
    public const int Post = 5000;
    public const int Comment = 1000;
    public const int Message = 2000;

    public const int EventTitle = 150;
    public const int EventLocation = 200;
    public const int EventDescription = 2000;

    public const int GroupName = 100;
    public const int GroupDescription = 1000;

    public const int ListingTitle = 150;
    public const int ListingCategory = 50;
    public const int ListingLocation = 200;
    public const int ListingDescription = 2000;
}
