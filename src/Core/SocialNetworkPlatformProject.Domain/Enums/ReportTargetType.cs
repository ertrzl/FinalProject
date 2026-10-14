using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Domain.Enums;

// What a report is about.
// Sent and read as its name ("Post", "Spam"...) in the JSON of the admin API.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportTargetType
{
    Post = 1,
    Comment = 2,
    User = 3,
    Listing = 4
}
