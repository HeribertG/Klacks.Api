// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Persistence;

/// <summary>
/// The one translation of EF Core write failures into the Domain exceptions Application code catches: a concurrency
/// failure becomes ConcurrencyException, every other DbUpdateException a DatabaseUpdateException whose duplicate and
/// foreign-key flags come from the PostgreSQL SQLSTATE (locale-independent, unlike the localized server message, e.g.
/// "doppelter Schlüsselwert" on a German-locale server never contains "duplicate"). The EF exception stays the inner
/// exception.
/// </summary>
internal static class DbUpdateExceptionTranslator
{
    private const string UniqueViolationSqlState = "23505";
    private const string ForeignKeyViolationSqlState = "23503";
    private const string ConcurrencyMessage = "The record was modified by another user. Please refresh and try again.";

    /// <summary>
    /// Translates an EF Core save failure into a ConcurrencyException or a DatabaseUpdateException flagged for duplicate and foreign-key violations.
    /// </summary>
    /// <param name="exception">The failure EF Core raised while saving</param>
    public static Exception Translate(DbUpdateException exception)
    {
        if (exception is DbUpdateConcurrencyException concurrency)
        {
            return ToConcurrencyException(concurrency);
        }

        var postgresException = exception.InnerException as PostgresException;
        var sqlState = postgresException?.SqlState;

        return new DatabaseUpdateException(
            exception.InnerException?.Message ?? string.Empty,
            exception,
            isDuplicate: sqlState == UniqueViolationSqlState,
            isForeignKeyViolation: sqlState == ForeignKeyViolationSqlState,
            constraintName: postgresException?.ConstraintName);
    }

    /// <summary>
    /// Converts an EF Core concurrency failure into a ConcurrencyException.
    /// </summary>
    /// <param name="exception">The concurrency failure EF Core raised while saving</param>
    public static ConcurrencyException ToConcurrencyException(DbUpdateConcurrencyException exception) =>
        new(ConcurrencyMessage, exception);
}
