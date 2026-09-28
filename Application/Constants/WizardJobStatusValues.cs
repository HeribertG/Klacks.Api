// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Status strings returned by the wizard/auto-wizard job status endpoints.
/// Mirrored by the frontend job status models.
/// </summary>
public static class WizardJobStatusValues
{
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
    public const string Unknown = "unknown";
}
