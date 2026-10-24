namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class GetEventPhotoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool CanDelete { get; set; }
}
