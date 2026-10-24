using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventPhotoService : IEventPhotoService
{
    private const string Folder = "eventphotos";

    private readonly IEventPhotoRepository _photos;
    private readonly IEventAttendeeRepository _attendees;
    private readonly IEventAccessService _access;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;

    public EventPhotoService(
        IEventPhotoRepository photos,
        IEventAttendeeRepository attendees,
        IEventAccessService access,
        IUserRepository users,
        IFileStorageService files)
    {
        _photos = photos;
        _attendees = attendees;
        _access = access;
        _users = users;
        _files = files;
    }

    public async Task<GetEventPhotosDto> GetAsync(Guid currentUserId, Guid eventId)
    {
        var ev = await _access.GetViewableAsync(currentUserId, eventId);
        return await BuildAsync(ev, currentUserId);
    }

    public async Task<GetEventPhotosDto> AddAsync(Guid currentUserId, Guid eventId, PostEventPhotosDto dto)
    {
        var ev = await _access.GetViewableAsync(currentUserId, eventId);

        if (!await CanUploadAsync(ev, currentUserId))
            throw new ForbiddenException(Loc.T("eventPhoto.uploadDenied"));

        var adding = dto.Photos.Count;
        if (await _photos.CountByUploaderAsync(eventId, currentUserId) + adding > EventLimits.MaxPhotosPerPerson)
            throw new BadRequestException(Loc.T("eventPhoto.personLimit", EventLimits.MaxPhotosPerPerson));
        if (await _photos.CountByEventAsync(eventId) + adding > EventLimits.MaxPhotosPerEvent)
            throw new BadRequestException(Loc.T("eventPhoto.eventLimit", EventLimits.MaxPhotosPerEvent));

        var added = new List<EventPhoto>();
        try
        {
            foreach (var file in dto.Photos)
            {
                var url = await _files.SaveImageAsync(file, Folder);
                added.Add(new EventPhoto { EventId = eventId, UploadedByUserId = currentUserId, Url = url });
            }

            foreach (var photo in added)
                await _photos.AddAsync(photo);
            await _photos.SaveChangesAsync();
        }
        catch
        {
            foreach (var photo in added)
                _files.Delete(photo.Url);
            throw;
        }

        var overPerson = await _photos.CountByUploaderAsync(eventId, currentUserId) > EventLimits.MaxPhotosPerPerson;
        var overEvent = await _photos.CountByEventAsync(eventId) > EventLimits.MaxPhotosPerEvent;
        if (overPerson || overEvent)
        {
            foreach (var photo in added)
                _photos.Delete(photo);
            await _photos.SaveChangesAsync();

            foreach (var photo in added)
                _files.Delete(photo.Url);

            throw new BadRequestException(overPerson
                ? Loc.T("eventPhoto.personLimit", EventLimits.MaxPhotosPerPerson)
                : Loc.T("eventPhoto.eventLimit", EventLimits.MaxPhotosPerEvent));
        }

        return await BuildAsync(ev, currentUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid eventId, Guid photoId)
    {
        var ev = await _access.GetViewableAsync(currentUserId, eventId);

        var photo = await _photos.GetByIdAsync(photoId);
        if (photo == null || photo.EventId != eventId)
            throw new NotFoundException(Loc.T("eventPhoto.notFound"));

        if (photo.UploadedByUserId != currentUserId && ev.CreatedByUserId != currentUserId)
            throw new ForbiddenException(Loc.T("eventPhoto.deleteDenied"));

        _photos.Delete(photo);
        await _photos.SaveChangesAsync();
        _files.Delete(photo.Url);
    }

    private async Task<bool> CanUploadAsync(Event ev, Guid userId)
    {
        return ev.CreatedByUserId == userId
            || await _attendees.AnyAsync(a => a.EventId == ev.Id && a.UserId == userId && a.Status == EventAttendeeStatus.Going);
    }

    private async Task<GetEventPhotosDto> BuildAsync(Event ev, Guid viewerId)
    {
        var photos = await _photos.GetByEventAsync(ev.Id);
        var people = await _users.GetSummariesAsync(photos.Select(p => p.UploadedByUserId));

        var canUpload = await CanUploadAsync(ev, viewerId);
        var mine = photos.Count(p => p.UploadedByUserId == viewerId);

        return new GetEventPhotosDto
        {
            Items = photos.Select(p => new GetEventPhotoDto
            {
                Id = p.Id,
                Url = p.Url,
                UploadedByUserId = p.UploadedByUserId,
                UploadedByName = people.TryGetValue(p.UploadedByUserId, out var person) ? person.FullName : string.Empty,
                CreatedAt = p.CreatedAt,
                CanDelete = p.UploadedByUserId == viewerId || ev.CreatedByUserId == viewerId
            }).ToList(),
            TotalCount = photos.Count,
            CanUpload = canUpload,
            MyCount = mine,
            MyRemaining = canUpload ? Math.Max(Math.Min(EventLimits.MaxPhotosPerPerson - mine, EventLimits.MaxPhotosPerEvent - photos.Count), 0) : 0,
            MaxPerPerson = EventLimits.MaxPhotosPerPerson,
            MaxPerEvent = EventLimits.MaxPhotosPerEvent
        };
    }
}
