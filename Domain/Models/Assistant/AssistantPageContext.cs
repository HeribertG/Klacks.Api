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

    public List<string>? SelectedClientIds { get; set; }

    public string? SelectedEntityType { get; set; }

    public bool HasAny()
    {
        return !string.IsNullOrWhiteSpace(CurrentRoute)
            || !string.IsNullOrWhiteSpace(SelectedGroupId)
            || !string.IsNullOrWhiteSpace(SelectedPeriodFrom)
            || !string.IsNullOrWhiteSpace(SelectedPeriodUntil)
            || !string.IsNullOrWhiteSpace(SelectedClientId)
            || SelectedClientIds is { Count: > 0 };
    }

    /// <summary>
    /// Parses the frontend's multi-selection (string ids) into entity guids, dropping blanks and
    /// unparseable values. Returns null when no valid selection is present, so the skill context field
    /// stays null unless the user actually ticked rows.
    /// </summary>
    public IReadOnlyList<Guid>? GetSelectedEntityIds()
    {
        if (SelectedClientIds is not { Count: > 0 })
        {
            return null;
        }

        var ids = new List<Guid>();
        foreach (var raw in SelectedClientIds)
        {
            if (Guid.TryParse(raw, out var id))
            {
                ids.Add(id);
            }
        }

        return ids.Count > 0 ? ids : null;
    }
}
