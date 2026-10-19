namespace SocialNetworkPlatformProject.Domain.Enums;

// Sold listings drop out of the public marketplace grid (unless the viewer asks to see them) but stay visible to
// their seller under "İlanlarım". Reserved ones stay in the grid with a badge: promised to someone, not final yet.
public enum ListingStatus
{
    Active = 0,
    Sold = 1,
    Reserved = 2
}
