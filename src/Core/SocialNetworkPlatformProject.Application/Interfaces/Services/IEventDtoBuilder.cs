using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventDtoBuilder
{
    GetEventDto Build(Event source, Guid viewerId);

    Task<List<GetEventDto>> BuildManyAsync(IEnumerable<Event> events, Guid viewerId);

    Task<GetEventDto> BuildDetailAsync(Event source, Guid viewerId);
}
