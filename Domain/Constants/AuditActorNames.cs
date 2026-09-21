// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The two non-user strings that end up in BaseEntity's CurrentUserCreated/Updated/Deleted audit
/// columns: the DataBaseContext fallback when no HTTP request carries a NameIdentifier claim, and the
/// seed/installer actor. Any code that reads an audit column back as a person - the approval chain's
/// "last planner" lookup above all - has to recognise both as "nobody", and a constant is the only way
/// the writer and the reader are guaranteed to agree on the spelling.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AuditActorNames
{
    public const string Anonymous = "Anonymous";

    public const string System = "System";

    public static bool IsPerson(string? actor) =>
        !string.IsNullOrWhiteSpace(actor)
        && !string.Equals(actor, Anonymous, StringComparison.Ordinal)
        && !string.Equals(actor, System, StringComparison.Ordinal);
}
