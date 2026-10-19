namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// The whole album of an event for one viewer: the pictures (newest first) and what this viewer may still do.
public class GetEventPhotosDto
{
    public List<GetEventPhotoDto> Items { get; set; } = new();
    public int TotalCount { get; set; }

    // The organizer and the people who are going may add pictures, within the limits below. The server decides;
    // the page only shows or hides the button.
    public bool CanUpload { get; set; }
    public int MyCount { get; set; }
    public int MyRemaining { get; set; }       // how many more this viewer may add (the smaller of the per-person and per-event room)
    public int MaxPerPerson { get; set; }
    public int MaxPerEvent { get; set; }
}
