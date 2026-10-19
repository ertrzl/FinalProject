namespace SocialNetworkPlatformProject.Domain.Enums;

// How a Finished game ended.
public enum GameOutcome
{
    Win = 0,      // three in a row
    Draw = 1,     // the board is full
    Resigned = 2  // one player gave up; the other one wins
}
