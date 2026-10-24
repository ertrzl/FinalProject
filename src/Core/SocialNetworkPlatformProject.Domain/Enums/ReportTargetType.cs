using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportTargetType
{
    Post = 1,
    Comment = 2,
    User = 3,
    Listing = 4
}
