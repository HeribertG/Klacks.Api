// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// String constants for AgentPlan.Status values. Keeps the executor and tests free of magic strings.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Planning;

public static class PlanStatus
{
    public const string Drafting = "drafting";
    public const string Executing = "executing";
    public const string PausedForApproval = "paused_for_approval";
    public const string Completed = "completed";
    public const string Aborted = "aborted";
    public const string Failed = "failed";
}
