namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

public class GetPollDto
{
    public List<GetPollOptionDto> Options { get; set; } = new();
    public int TotalVotes { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsEnded { get; set; }
    public Guid? MyOptionId { get; set; }
    public bool ShowResults { get; set; }
    public bool CanVote { get; set; }
}

public class GetPollOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public int Percent { get; set; }
}
