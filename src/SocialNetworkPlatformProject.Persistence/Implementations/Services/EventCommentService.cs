using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventCommentService : IEventCommentService
{
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private readonly IEventCommentRepository _comments;
    private readonly IEventAccessService _access;
    private readonly IUserRepository _users;
    private readonly IEventNotifier _notifier;

    public EventCommentService(
        IEventCommentRepository comments,
        IEventAccessService access,
        IUserRepository users,
        IEventNotifier notifier)
    {
        _comments = comments;
        _access = access;
        _users = users;
        _notifier = notifier;
    }

    public async Task<PagedResult<GetEventCommentDto>> GetAsync(Guid currentUserId, Guid eventId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var found = await _access.GetViewableAsync(currentUserId, eventId);

        var query = _comments.GetAll(c => c.EventId == eventId, asNoTracking: true);
        var total = await query.CountAsync();

        // Announcements stay pinned on top; everything else is newest first.
        var items = await query
            .OrderByDescending(c => c.IsAnnouncement)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var authors = await _users.GetSummariesAsync(items.Select(c => c.AuthorId));

        return new PagedResult<GetEventCommentDto>
        {
            Items = items.Select(c => ToDto(c, authors.GetValueOrDefault(c.AuthorId), currentUserId, found.CreatedByUserId)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GetEventCommentDto> CreateAsync(Guid currentUserId, Guid eventId, PostEventCommentDto dto)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (dto.IsAnnouncement)
        {
            if (found.CreatedByUserId != currentUserId)
                throw new ForbiddenException("Duyuruyu sadece etkinliği düzenleyen yapabilir.");

            if (found.EndsAt <= DateTime.UtcNow)
                throw new BadRequestException("Sona ermiş bir etkinlik için duyuru yapılamaz.");
        }

        var comment = new EventComment
        {
            EventId = eventId,
            AuthorId = currentUserId,
            Content = dto.Content.Trim(),
            IsAnnouncement = dto.IsAnnouncement
        };

        await _comments.AddAsync(comment);
        await _comments.SaveChangesAsync();

        // A plain comment is news to the organizer; their own comments and announcements aren't.
        if (dto.IsAnnouncement)
            await _notifier.AnnouncementAsync(found, currentUserId);
        else if (found.CreatedByUserId != currentUserId)
            await _notifier.CommentAddedAsync(found, comment.Id, currentUserId);

        var author = await _users.GetSummaryAsync(currentUserId);
        return ToDto(comment, author, currentUserId, found.CreatedByUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid eventId, Guid commentId)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId);

        var comment = await _comments.GetAll(c => c.Id == commentId && c.EventId == eventId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Comment not found.");

        if (comment.AuthorId != currentUserId && found.CreatedByUserId != currentUserId)
            throw new ForbiddenException("Bu mesajı silme yetkin yok.");

        _comments.Delete(comment);
        await _comments.SaveChangesAsync();
    }

    private static GetEventCommentDto ToDto(EventComment comment, UserSummary? author, Guid currentUserId, Guid organizerId)
    {
        return new GetEventCommentDto
        {
            Id = comment.Id,
            AuthorId = comment.AuthorId,
            AuthorName = author?.FullName ?? string.Empty,
            AuthorAvatarUrl = author?.AvatarUrl,
            Content = comment.Content,
            IsAnnouncement = comment.IsAnnouncement,
            CanDelete = comment.AuthorId == currentUserId || organizerId == currentUserId,
            CreatedAt = comment.CreatedAt
        };
    }
}
