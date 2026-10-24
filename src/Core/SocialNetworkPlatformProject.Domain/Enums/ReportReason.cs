using System.Text.Json.Serialization;

namespace SocialNetworkPlatformProject.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportReason
{
    Spam = 1,
    Harassment = 2,
    Inappropriate = 3,
    Fake = 4,
    Other = 5
}
