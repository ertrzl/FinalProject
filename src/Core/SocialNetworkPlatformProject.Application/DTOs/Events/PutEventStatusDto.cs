namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// events.html "Katılıyorum" / "İlgileniyorum" buttons; "None" removes the current user's response.
public class PutEventStatusDto
{
    public string Status { get; set; } = "None"; // "Going" / "Interested" / "None"
}
