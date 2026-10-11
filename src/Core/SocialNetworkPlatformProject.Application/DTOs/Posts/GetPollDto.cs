namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// The poll under a post's text. The server decides what is shown: the counts and percentages only come with
// ShowResults (after you voted, once the poll has ended, or for its author); before that every count is 0.
public class GetPollDto
{
    public List<GetPollOptionDto> Options { get; set; } = new();
    public int TotalVotes { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsEnded { get; set; }
    public Guid? MyOptionId { get; set; }
    public bool ShowResults { get; set; }
    public bool CanVote { get; set; } // not ended and not voted yet
}

public class GetPollOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public int Percent { get; set; } // of all votes, rounded
}
