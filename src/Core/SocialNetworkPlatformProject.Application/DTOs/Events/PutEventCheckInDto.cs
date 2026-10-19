namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html "Yoklama": ticking (true) or un-ticking (false) one person.
public class PutEventCheckInDto
{
    public bool CheckedIn { get; set; }
}
