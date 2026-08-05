// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, DB-free extraction of the entity a successful skill created or updated, from its
/// <see cref="SkillResult.Data"/> payload. Deliberately conservative: it fires only for a small curated
/// allow-list of skills whose result payload carries their OWN entity id, and reads that id (and an
/// optional display name) generically from the serialized JSON. Skills not on the list return nothing —
/// this is why relationship skills like add_client_to_group (payload carries a foreign ClientId, not the
/// created GroupItem) and bulk skills like add_break/place_work (payload carries no Break/Work id) never
/// register a guessed, wrong entity.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class RecentEntityExtractor
{
    private sealed record TrackedSkill(
        string EntityType,
        string[] IdProperties,
        string[] NameProperties,
        string[]? CompositeNameProperties,
        string Action);

    private static readonly IReadOnlyDictionary<string, TrackedSkill> TrackedSkills =
        new Dictionary<string, TrackedSkill>(StringComparer.OrdinalIgnoreCase)
        {
            ["create_shift"] = new("shift", new[] { "ShiftId" }, new[] { "Name" }, null, RecentEntityDefaults.ActionCreated),
            ["create_employee"] = new("client", new[] { "EmployeeId" }, Array.Empty<string>(), new[] { "FirstName", "LastName" }, RecentEntityDefaults.ActionCreated),
            ["create_group"] = new("group", new[] { "GroupId" }, new[] { "Name" }, null, RecentEntityDefaults.ActionCreated),
            ["create_contract"] = new("contract", new[] { "Id" }, new[] { "Name" }, null, RecentEntityDefaults.ActionCreated),
            ["add_expense"] = new("expenses", new[] { "Id" }, new[] { "Description" }, null, RecentEntityDefaults.ActionCreated),
            ["add_workchange"] = new("workchange", new[] { "Id" }, new[] { "Type" }, null, RecentEntityDefaults.ActionCreated),
            ["update_membership"] = new("membership", new[] { "MembershipId" }, Array.Empty<string>(), null, RecentEntityDefaults.ActionUpdated),
            ["update_client"] = new("client", new[] { "ClientId" }, Array.Empty<string>(), new[] { "FirstName", "LastName" }, RecentEntityDefaults.ActionUpdated),
        };

    public static bool TryExtract(string skillName, SkillResult result, out RecentEntityDescriptor? descriptor)
    {
        descriptor = null;

        if (string.IsNullOrWhiteSpace(skillName) || result is null || !result.Success || result.Data is null)
        {
            return false;
        }

        if (!TrackedSkills.TryGetValue(skillName, out var tracked))
        {
            return false;
        }

        JsonElement root;
        try
        {
            root = JsonSerializer.SerializeToElement(result.Data);
        }
        catch (Exception)
        {
            return false;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var entityId = ExtractGuid(root, tracked.IdProperties);
        if (entityId == null)
        {
            return false;
        }

        var displayName = ExtractDisplayName(root, tracked);

        descriptor = new RecentEntityDescriptor(tracked.EntityType, entityId.Value, displayName, tracked.Action);
        return true;
    }

    private static Guid? ExtractGuid(JsonElement root, IReadOnlyList<string> propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (TryGetProperty(root, name, out var element)
                && element.ValueKind == JsonValueKind.String
                && Guid.TryParse(element.GetString(), out var parsed)
                && parsed != Guid.Empty)
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? ExtractDisplayName(JsonElement root, TrackedSkill tracked)
    {
        if (tracked.CompositeNameProperties is { Length: > 0 })
        {
            var parts = new List<string>();
            foreach (var name in tracked.CompositeNameProperties)
            {
                if (TryGetProperty(root, name, out var element)
                    && element.ValueKind == JsonValueKind.String)
                {
                    var value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        parts.Add(value.Trim());
                    }
                }
            }

            if (parts.Count > 0)
            {
                return string.Join(' ', parts);
            }
        }

        foreach (var name in tracked.NameProperties)
        {
            if (TryGetProperty(root, name, out var element)
                && element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
