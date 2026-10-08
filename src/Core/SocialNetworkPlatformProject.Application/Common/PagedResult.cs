namespace SocialNetworkPlatformProject.Application.Common;

// Used by any list endpoint that needs paging (feed, search results, notifications...).
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Is there anything after this page? The pages use it to show or hide their "Daha fazla yükle" button.
    public bool HasMore => Page < TotalPages;
}
