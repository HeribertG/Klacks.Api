// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-turn UI state passed from the frontend so Klacksy can resolve "this group / this period / this client"
/// without asking back. All fields are optional; an absent field means "user has nothing selected for it".
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public class AssistantPageContext
{
    public string? CurrentRoute { get; set; }

    public string? SelectedGroupId { get; set; }

    public string? SelectedPeriodFrom { get; set; }

    public string? SelectedPeriodUntil { get; set; }

    public string? SelectedClientId { get; set; }

    public bool HasAny()
    {
        return !string.IsNullOrWhiteSpace(CurrentRoute)
            || !string.IsNullOrWhiteSpace(SelectedGroupId)
            || !string.IsNullOrWhiteSpace(SelectedPeriodFrom)
            || !string.IsNullOrWhiteSpace(SelectedPeriodUntil)
            || !string.IsNullOrWhiteSpace(SelectedClientId);
    }
}
