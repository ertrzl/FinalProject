using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventInviteService : IEventInviteService
{
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private readonly IEventInviteRepository _invites;
    private readonly IGroupRepository _groups;
    private readonly IEventAccessService _access;
    private readonly IEventService _events;
    private readonly IUserRepository _users;
    private readonly IEventNotifier _notifier;

    public EventInviteService(
        IEventInviteRepository invites,
        IGroupRepository groups,
        IEventAccessService access,
        IEventService events,
        IUserRepository users,
        IEventNotifier notifier)
    {
        _invites = invites;
        _groups = groups;
        _access = access;
        _events = events;
        _users = users;
        _notifier = notifier;
    }

    public async Task InviteAsync(Guid currentUserId, Guid eventId, PostEventInviteDto dto)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);

        if (found.EndsAt <= DateTime.UtcNow)
            throw new BadRequestException("Sona ermiş bir etkinliğe davet gönderilemez.");

        EnsureCanInvite(found, currentUserId);

        var targetId = dto.UserId;
        if (targetId == currentUserId)
            throw new BadRequestException("Kendini davet edemezsin.");

        if (!await _users.ExistsAsync(targetId))
            throw new NotFoundException("Kullanıcı bulunamadı.");

        if (targetId == found.CreatedByUserId || found.Attendees.Any(a => a.UserId == targetId))
            throw new BadRequestException("Bu kişi zaten etkinlikte.");

        // A group event is invisible to non-members, so inviting one would only send them to a 404.
        if (found.GroupId.HasValue && !await _access.IsGroupMemberAsync(found.GroupId.Value, targetId))
            throw new BadRequestException("Bu kişi grubun üyesi değil, grup etkinliğine davet edilemez.");

        if (await _invites.AnyAsync(i => i.EventId == eventId && i.InvitedUserId == targetId))
            throw new ConflictException("Bu kişiye zaten davet gönderilmiş.");

        await _invites.AddAsync(new EventInvite { EventId = eventId, InvitedUserId = targetId, InvitedByUserId = currentUserId });

        try
        {
            await _invites.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two people invited the same person at the same moment: the unique index let only one through.
            throw new ConflictException("Bu kişiye zaten davet gönderilmiş.");
        }

        await _notifier.InvitedAsync(found, targetId, currentUserId);
    }

    public async Task<List<GetEventInviteeDto>> GetInviteesAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _access.GetViewableAsync(currentUserId, eventId, AttendeeIncludes);
        EnsureCanInvite(found, currentUserId);

        var invites = await _invites.GetAll(i => i.EventId == eventId, orderBy: i => i.CreatedAt, isDescending: true, asNoTracking: true)
            .ToListAsync();

        var people = await _users.GetSummariesAsync(invites.SelectMany(i => new[] { i.InvitedUserId, i.InvitedByUserId }));

        return invites
            .Where(i => people.ContainsKey(i.InvitedUserId))
            .Select(i => new GetEventInviteeDto
            {
                UserId = i.InvitedUserId,
                FullName = people[i.InvitedUserId].FullName,
                AvatarUrl = people[i.InvitedUserId].AvatarUrl,
                InvitedByName = people.TryGetValue(i.InvitedByUserId, out var inviter) ? inviter.FullName : string.Empty
            })
            .ToList();
    }

    public async Task<List<GetEventInviteDto>> GetMyInvitesAsync(Guid currentUserId)
    {
        var now = DateTime.UtcNow;

        var invites = await _invites.GetAll(
                filter: i => i.InvitedUserId == currentUserId && i.Event!.EndsAt > now,
                orderBy: i => i.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: "Event")
            .ToListAsync();

        // An invite to a group event is only useful while I'm still in that group.
        var myGroupIds = await _access.GetMyGroupIdsAsync(currentUserId);
        invites = invites.Where(i => !i.Event!.GroupId.HasValue || myGroupIds.Contains(i.Event.GroupId.Value)).ToList();

        var inviters = await _users.GetSummariesAsync(invites.Select(i => i.InvitedByUserId));

        var groupIds = invites.Where(i => i.Event!.GroupId.HasValue).Select(i => i.Event!.GroupId!.Value).Distinct().ToList();
        var groupNames = groupIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _groups.GetAll(g => groupIds.Contains(g.Id), asNoTracking: true).ToDictionaryAsync(g => g.Id, g => g.Name);

        return invites.Select(i =>
        {
            inviters.TryGetValue(i.InvitedByUserId, out var inviter);
            return new GetEventInviteDto
            {
                EventId = i.EventId,
                Title = i.Event!.Title,
                StartsAt = i.Event.StartsAt,
                EndsAt = i.Event.EndsAt,
                CoverImageUrl = i.Event.CoverImageUrl,
                IsOnline = i.Event.IsOnline,
                Location = i.Event.Location,
                GroupName = i.Event.GroupId.HasValue ? groupNames.GetValueOrDefault(i.Event.GroupId.Value) : null,
                InvitedByUserId = i.InvitedByUserId,
                InvitedByName = inviter?.FullName ?? string.Empty,
                InvitedByAvatarUrl = inviter?.AvatarUrl,
                InvitedAt = i.CreatedAt
            };
        }).ToList();
    }

    public async Task<GetEventDto> AcceptAsync(Guid currentUserId, Guid eventId)
    {
        if (!await _invites.AnyAsync(i => i.EventId == eventId && i.InvitedUserId == currentUserId))
            throw new NotFoundException("Davet bulunamadı.");

        // Accepting is just "Katılıyorum": capacity, started-event and visibility rules all apply, and
        // SetStatusAsync deletes the invite in the same save.
        return await _events.SetStatusAsync(currentUserId, eventId, new PutEventStatusDto { Status = nameof(EventAttendeeStatus.Going) });
    }

    public async Task DeclineAsync(Guid currentUserId, Guid eventId)
    {
        var invite = await _invites.GetAll(i => i.EventId == eventId && i.InvitedUserId == currentUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Davet bulunamadı.");

        _invites.Delete(invite);
        await _invites.SaveChangesAsync();
    }

    // The organizer and people who said "Katılıyorum" may invite (and see who has been invited); a private
    // event stays in the organizer's hands, so only they may.
    private static void EnsureCanInvite(Event ev, Guid userId)
    {
        var isOrganizer = ev.CreatedByUserId == userId;
        if (ev.IsPrivate && !isOrganizer)
            throw new ForbiddenException("Özel bir etkinliğe sadece etkinliği düzenleyen davet gönderebilir.");

        var isGoing = ev.Attendees.Any(a => a.UserId == userId && a.Status == EventAttendeeStatus.Going);
        if (!isOrganizer && !isGoing)
            throw new ForbiddenException("Davet göndermek için etkinliği düzenleyen ya da katılan biri olmalısın.");
    }
}
