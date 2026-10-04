using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

// What the start time may be depends on whether the event has already started (an ongoing event keeps its start),
// so that rule lives in EventService.UpdateAsync rather than here.
public class PutEventDtoValidator : EventInputValidator<PutEventDto>
{
}
