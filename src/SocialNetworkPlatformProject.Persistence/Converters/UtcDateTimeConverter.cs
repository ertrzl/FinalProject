using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SocialNetworkPlatformProject.Persistence.Converters;

// SQL Server's datetime2 has no time zone, so EF reads values back as Kind=Unspecified and the API would serialize
// them without a "Z". Marking them UTC on the way out makes JSON carry the offset the browser needs.
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
