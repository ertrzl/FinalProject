using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

// What an event card needs to know about who is coming, counted by the database: how many people are going,
// interested or waiting, and where the viewer stands (their status and their place in the waiting line).
public record EventAttendance(int Going, int Interested, int Waitlisted, EventAttendeeStatus? MyStatus, int MyWaitlistPosition)
{
    public static readonly EventAttendance None = new(0, 0, 0, null, 0);
}
