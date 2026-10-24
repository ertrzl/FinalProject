using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SocialNetworkPlatformProject.Persistence.Extensions;

public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
