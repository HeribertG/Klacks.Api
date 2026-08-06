// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Identity the e-mail automation acts under. The owner decision was a dedicated service account rather
/// than "whichever admin happened to enable the automation", so that the audit trail names the
/// automation itself and revoking one person's admin role does not silently change what the automation
/// may do. Left empty the automation falls back to the admin it already resolves for its autonomy
/// threshold — same behaviour as before, but under a real token instead of synthesised admin rights.
/// </summary>

namespace Klacks.Api.Application.Configuration;

public class EmailAutomationOptions
{
    public const string SectionName = "EmailAutomation";

    public string ServiceAccountId { get; set; } = string.Empty;
}
