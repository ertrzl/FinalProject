namespace SocialNetworkPlatformProject.Application.Common;

public static class MarketplaceLimits
{
    public const int MaxImagesPerListing = 5;

    // A listing is visible for this long; "Yenile" starts the period again.
    public const int ListingLifetimeDays = 30;

    // "Yenile" is offered (and accepted) only for an expired listing or one that is about to expire, and at most once per cooldown.
    public const int RenewWindowDays = 7;
    public const int RenewCooldownHours = 24;

    // "Öne Çıkar": one listing of a seller at a time, for a day, and a seller can do it once a week.
    public const int FeatureHours = 24;
    public const int FeatureCooldownDays = 7;

    public const int MaxSimilarListings = 6;
}
