using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SocialNetworkPlatformProject.Persistence.Converters;

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
