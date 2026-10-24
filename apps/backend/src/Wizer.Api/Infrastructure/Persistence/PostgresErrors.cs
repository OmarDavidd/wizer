using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Wizer.Api.Infrastructure.Persistence;

public static class PostgresErrors
{
    /// <summary>True when the save failed only because a unique constraint already holds the value.</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
