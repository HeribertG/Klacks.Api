// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Typed Npgsql parameter for the geocoding upsert SQL. The explicit type matters for null values: an untyped
/// NULL in an INSERT ... SELECT would reach PostgreSQL as text and fail against uuid or double columns.
/// </summary>

using Npgsql;
using NpgsqlTypes;

namespace Klacks.Api.Infrastructure.Repositories.Geocoding;

internal static class GeocodingSqlParameter
{
    public static NpgsqlParameter Create(string name, NpgsqlDbType type, object? value) =>
        new(name, type) { Value = value ?? DBNull.Value };
}
