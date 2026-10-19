namespace SocialNetworkPlatformProject.Domain.Enums;

// Invited and Active are the two "open" states: a pair of friends can have only one open game at a time.
public enum GameStatus
{
    Invited = 0,
    Active = 1,
    Finished = 2,
    Declined = 3,  // the invited friend said no
    Cancelled = 4, // the inviter took the invitation back
    Expired = 5    // nobody answered the invitation in time, or nobody moved for a week
}
