using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Domain.Enums;

// The reasons a person can pick when reporting something.
// Sent and read as its name ("Post", "Spam"...) in the JSON of the admin API.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportReason
{
    Spam = 1,
    Harassment = 2,
    Inappropriate = 3,
    Fake = 4,
    Other = 5
}
