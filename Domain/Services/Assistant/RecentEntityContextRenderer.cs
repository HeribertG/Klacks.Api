// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the compact system-context block listing the entities created or updated earlier in the
/// current conversation, so the model can resolve implicit follow-up references itself (no phrase
/// detector). Returns an empty string when there is nothing to show, so callers can omit the block.
/// </summary>

using System.Text;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecentEntityContextRenderer
{
    private const string Header =
        "[RECENTLY_TOUCHED] Entities you created or updated earlier in THIS conversation, most recent first. " +
        "When the user refers to one implicitly (\"it\", \"that one\", \"the last one\", \"the one I just created/edited\"), " +
        "resolve the reference to the matching entry below and use its id in the follow-up skill call:";

    public static string Render(IReadOnlyList<RecentEntityRow>? rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var row in rows)
        {
            var name = string.IsNullOrWhiteSpace(row.DisplayName)
                ? string.Empty
                : $" \"{row.DisplayName}\"";
            sb.AppendLine($"- {row.EntityType}{name} (id {row.EntityId}) — {row.Action}");
        }

        return sb.ToString().TrimEnd();
    }
}
