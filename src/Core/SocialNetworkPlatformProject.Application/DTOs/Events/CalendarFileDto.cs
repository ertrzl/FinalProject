namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html's "Takvime Ekle" (.ics download)
public class CalendarFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
