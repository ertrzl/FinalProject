using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Pending = 0,
    Resolved = 1,
    Dismissed = 2
}
