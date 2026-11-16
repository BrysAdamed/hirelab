using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// Reads which unique index or check constraint a failed save hit, so the code that writes the row can map a lost race
/// to a readable <c>409</c> (ADR 0004).
/// </summary>
public static class ConstraintViolations
{
    /// <summary>The name of the index or constraint a failed save violated, or <c>null</c> for any other failure.</summary>
    public static string? ViolatedConstraint(this DbUpdateException exception) =>
        exception?.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation,
        } postgres
            ? postgres.ConstraintName
            : null;
}
