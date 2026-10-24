using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

public record EventAttendance(int Going, int Interested, int Waitlisted, EventAttendeeStatus? MyStatus, int MyWaitlistPosition, bool MyCheckedIn = false)
{
    public static readonly EventAttendance None = new(0, 0, 0, null, 0);
}
