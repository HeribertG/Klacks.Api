// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes the ImportContentHash for a region-setup entity-import row: a stable SHA-256 hex digest over
/// the row's editable value fields, joined with a separator that cannot appear inside a single field
/// value (the fields are numbers/enum names). Never hash ImportSourceKey or audit fields — only the
/// values a customer could edit, so an edit is exactly what changes the hash.
/// </summary>

using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Application.Services.Setup;

public static class ImportContentHasher
{
    private const string FieldSeparator = "|";

    public static string ComputeHash(params string[] fields)
    {
        var joined = string.Join(FieldSeparator, fields);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined)));
    }
}
