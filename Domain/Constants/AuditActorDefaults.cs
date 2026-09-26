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

    /// <summary>
    /// Written into PerformedByName when Klacksy acted unattended on the standing consent of an admin
    /// (the autonomous period close). PerformedBy still carries that admin's id, so the audit names both
    /// who released the action and that nobody clicked it.
    /// </summary>
    public const string AutonomousActorName = "Klacksy (autonomous)";
}
