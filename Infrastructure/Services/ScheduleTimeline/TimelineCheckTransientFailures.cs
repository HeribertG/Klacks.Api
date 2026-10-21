// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Retry policy of the schedule timeline worker: which failures of a check count as transient (walking the
/// inner-exception chain), how many retries a job gets and how long the worker waits before one.
/// </summary>

using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public static class TimelineCheckTransientFailures
{
    public const int MaxRetries = 1;

    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan RetryJitter = TimeSpan.FromMilliseconds(250);

    private static readonly HashSet<string> TransientPostgresSqlStates = new(StringComparer.Ordinal)
    {
        PostgresErrorCodes.SerializationFailure,
        PostgresErrorCodes.DeadlockDetected,
        PostgresErrorCodes.ConnectionException,
        PostgresErrorCodes.ConnectionDoesNotExist,
        PostgresErrorCodes.ConnectionFailure,
        PostgresErrorCodes.SqlServerRejectedEstablishmentOfSqlConnection,
        PostgresErrorCodes.CannotConnectNow,
    };

    public static bool IsTransient(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case TimeoutException:
                case DbUpdateConcurrencyException:
                case SocketException:
                    return true;
                case PostgresException pg when TransientPostgresSqlStates.Contains(pg.SqlState):
                    return true;
                case NpgsqlException:
                    return true;
            }
        }
        return false;
    }

    public static TimeSpan NextRetryDelay()
        => RetryBaseDelay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)RetryJitter.TotalMilliseconds));
}
