namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class GetEventPhotosDto
{
    public List<GetEventPhotoDto> Items { get; set; } = new();
    public int TotalCount { get; set; }

    public bool CanUpload { get; set; }
    public int MyCount { get; set; }
    public int MyRemaining { get; set; }
    public int MaxPerPerson { get; set; }
    public int MaxPerEvent { get; set; }
}
