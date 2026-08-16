// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Fallback values written into audit records when the acting identity cannot be read from the
/// current principal. Kept as constants so the stored marker stays greppable and identical across
/// every audit writer.
/// </summary>
public static class AuditActorDefaults
{
    /// <summary>Written into PerformedBy when the NameIdentifier claim is missing.</summary>
    public const string UnknownActor = "Unknown";
}
