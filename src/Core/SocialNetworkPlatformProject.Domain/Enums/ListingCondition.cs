namespace SocialNetworkPlatformProject.Domain.Enums;

// How worn the item is. Numbering starts at 1 on purpose: 0 means "never chosen", which lets the database default
// ("Good") fill in the listings that existed before this field did.
public enum ListingCondition
{
    New = 1,     // Sıfır
    LikeNew = 2, // Sıfır gibi
    Good = 3,    // İyi
    Fair = 4     // Orta
}
