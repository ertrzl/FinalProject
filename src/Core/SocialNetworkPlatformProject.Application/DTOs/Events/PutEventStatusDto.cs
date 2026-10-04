namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// events.html "Katılıyorum" / "İlgileniyorum" / "Bekleme listesine katıl" buttons; "None" removes the current user's response.
public class PutEventStatusDto
{
    public string Status { get; set; } = "None"; // "Going" / "Interested" / "Waitlisted" / "None"
}
