using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventService : IEventService
{
    // GetEventDto's Going/Interested counts are computed from the Attendees collection.
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    private readonly IEventRepository _events;
    private readonly IEventAttendeeRepository _attendees;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public EventService(
        IEventRepository events,
        IEventAttendeeRepository attendees,
        IFileStorageService files,
        IMapper mapper)
    {
        _events = events;
        _attendees = attendees;
        _files = files;
        _mapper = mapper;
    }

    public async Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto)
    {
        string? coverUrl = null;
        if (dto.CoverImage != null)
            coverUrl = await _files.SaveImageAsync(dto.CoverImage, "events");

        var newEvent = new Event
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location?.Trim(),
            CoverImageUrl = coverUrl,
            StartsAt = dto.StartsAt,
            CreatedByUserId = currentUserId
        };

        await _events.AddAsync(newEvent);
        await _events.SaveChangesAsync();

        return ToDto(newEvent, currentUserId);
    }

    public async Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _events.GetByIdAsync(eventId, AttendeeIncludes)
            ?? throw new NotFoundException("Event not found.");

        return ToDto(found, currentUserId);
    }

    public async Task<List<GetEventDto>> GetUpcomingAsync(Guid currentUserId)
    {
        var now = DateTime.UtcNow;

        var upcoming = await _events.GetAll(
                filter: e => e.StartsAt >= now,
                orderBy: e => e.StartsAt,
                asNoTracking: true,
                includes: AttendeeIncludes)
            .ToListAsync();

        return upcoming.Select(e => ToDto(e, currentUserId)).ToList();
    }

    public async Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto)
    {
        var found = await _events.GetByIdAsync(eventId, AttendeeIncludes)
            ?? throw new NotFoundException("Event not found.");

        var existing = found.Attendees.FirstOrDefault(a => a.UserId == currentUserId);

        if (dto.Status == "None")
        {
            if (existing != null)
            {
                _attendees.Delete(existing);
                found.Attendees.Remove(existing);
            }
        }
        else
        {
            var status = Enum.Parse<EventAttendeeStatus>(dto.Status);
            if (existing != null)
            {
                existing.Status = status;
            }
            else
            {
                var attendee = new EventAttendee { EventId = eventId, UserId = currentUserId, Status = status };
                await _attendees.AddAsync(attendee);
            }
        }

        await _attendees.SaveChangesAsync();

        return ToDto(found, currentUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid eventId)
    {
        var found = await _events.GetByIdAsync(eventId)
            ?? throw new NotFoundException("Event not found.");

        if (found.CreatedByUserId != currentUserId)
            throw new ForbiddenException("Only the creator can delete this event.");

        _events.Delete(found);
        await _events.SaveChangesAsync();

        _files.Delete(found.CoverImageUrl);
    }

    private GetEventDto ToDto(Event source, Guid currentUserId)
    {
        var dto = _mapper.Map<GetEventDto>(source);
        var mine = source.Attendees.FirstOrDefault(a => a.UserId == currentUserId);
        dto.CurrentUserStatus = mine?.Status.ToString() ?? "None";
        return dto;
    }
}
