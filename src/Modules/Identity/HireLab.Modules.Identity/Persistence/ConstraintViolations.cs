using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HireLab.Modules.Identity.Persistence;

internal static class ConstraintViolations
{
    /// <summary>The name of the index or constraint a failed save violated, or <c>null</c> for any other failure.</summary>
    public static string? ViolatedConstraint(this DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation,
        } postgres
            ? postgres.ConstraintName
            : null;
}
