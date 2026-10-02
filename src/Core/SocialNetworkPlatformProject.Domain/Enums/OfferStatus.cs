namespace SocialNetworkPlatformProject.Domain.Enums;

// Lifecycle of one price negotiation between a buyer and a seller over a listing.
public enum OfferStatus
{
    Open = 0,      // waiting for the party whose turn it is
    Accepted = 1,  // deal made, listing marked Sold
    Rejected = 2,  // the party whose turn it was said no
    Withdrawn = 3, // the party who made the last proposal took it back
    Closed = 4     // the listing was sold to someone else (or marked sold) while the negotiation was open
}
