using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SocialNetworkPlatformProject.Persistence.Extensions;

public static class DbUpdateExceptionExtensions
{
    // A unique index or constraint refused the row (SQL Server error 2601 / 2627): somebody else got there first.
    public static bool IsUniqueViolation(this DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
