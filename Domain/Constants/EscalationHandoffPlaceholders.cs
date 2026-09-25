// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Names of the {{placeholders}} the escalation handoff texts carry. Date and employee describe the shift
/// an absence chain is about, action and finding the remediation and the finding an approval chain is about,
/// responder the colleague who acknowledged. The values are ready to insert.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class EscalationHandoffPlaceholders
{
    public const string Date = "date";
    public const string Employee = "employee";
    public const string Responder = "responder";
    public const string Action = "action";
    public const string Finding = "finding";
}
