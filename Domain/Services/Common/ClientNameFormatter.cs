// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats a Client's display name in "Last, First" order, trimming and
/// gracefully handling missing parts. Used wherever a stable Client identity
/// label is rendered into exports, audit entries, and lookup dropdowns.
/// </summary>
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public static class ClientNameFormatter
{
    public static string LastFirst(Client? client)
    {
        if (client == null) return string.Empty;
        return LastFirst(client.Name, client.FirstName);
    }

    public static string LastFirst(string? lastName, string? firstName)
    {
        var ln = (lastName ?? string.Empty).Trim();
        var fn = (firstName ?? string.Empty).Trim();
        if (ln.Length == 0 && fn.Length == 0) return string.Empty;
        if (ln.Length == 0) return fn;
        if (fn.Length == 0) return ln;
        return $"{ln}, {fn}";
    }
}
