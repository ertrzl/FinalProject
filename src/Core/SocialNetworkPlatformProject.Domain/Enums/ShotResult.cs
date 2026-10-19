namespace SocialNetworkPlatformProject.Domain.Enums;

// What a shot in battleship did.
public enum ShotResult
{
    Miss = 0,
    Hit = 1,
    Sunk = 2 // a hit that took the last unhit cell of a ship
}
