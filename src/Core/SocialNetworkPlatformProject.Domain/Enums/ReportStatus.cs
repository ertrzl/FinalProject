using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Domain.Enums;

// Sent and read as its name ("Post", "Spam"...) in the JSON of the admin API.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Pending = 0,
    // An admin looked at it and acted (or agreed with it).
    Resolved = 1,
    // An admin looked at it and found nothing wrong.
    Dismissed = 2
}
