namespace SocialNetworkPlatformProject.Domain.Enums;

// Matches events.html buttons: "Katılıyorum" / "İlgileniyorum" / "Bekleme listesine katıl".
// Stored as the int value, so new values are only ever appended.
public enum EventAttendeeStatus
{
    Going = 0,
    Interested = 1,
    Waitlisted = 2 // wants to go but the event is full; moves up to Going, first come first served, when a spot opens
}
