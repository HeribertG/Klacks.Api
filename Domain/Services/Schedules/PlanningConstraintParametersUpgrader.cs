// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Brings a stored PlanningConstraint.ParametersJson to the current schemaVersion before it is validated, so
/// rows written by an older Klacks version keep loading after a schema bump. Every bump of
/// PlanningConstraintDefaults.CurrentParametersSchemaVersion must add exactly one step "n -> n+1" to
/// <see cref="Steps"/>; a guard test fails while a step between the minimum supported and the current version
/// is missing. A version above the current one was written by a newer Klacks and is refused, never guessed.
/// </summary>

using System.Text.Json.Nodes;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Schedules;

public static class PlanningConstraintParametersUpgrader
{
    /// <summary>Upgrade step from the key version to the next one. Empty while only version 1 exists.</summary>
    public static readonly IReadOnlyDictionary<int, Func<JsonObject, JsonObject>> Steps =
        new Dictionary<int, Func<JsonObject, JsonObject>>();

    /// <summary>
    /// Upgrades <paramref name="parameters"/> to the current version (<paramref name="upgraded"/>). Returns false
    /// with an error when the version is missing, unsupported or newer than this Klacks version.
    /// </summary>
    public static bool TryUpgrade(JsonObject parameters, List<string> errors, out JsonObject upgraded)
    {
        upgraded = parameters;
        if (!TryReadVersion(parameters, out var version))
        {
            errors.Add($"'{PlanningConstraintDefaults.SchemaVersionProperty}' is required and must be a whole number.");
            return false;
        }

        if (version > PlanningConstraintDefaults.CurrentParametersSchemaVersion)
        {
            errors.Add($"'{PlanningConstraintDefaults.SchemaVersionProperty}' {version} was written by a newer Klacks version " +
                $"(this version understands up to {PlanningConstraintDefaults.CurrentParametersSchemaVersion}).");
            return false;
        }

        if (version < PlanningConstraintDefaults.MinimumSupportedParametersSchemaVersion)
        {
            errors.Add($"'{PlanningConstraintDefaults.SchemaVersionProperty}' {version} is no longer supported " +
                $"(minimum {PlanningConstraintDefaults.MinimumSupportedParametersSchemaVersion}).");
            return false;
        }

        while (version < PlanningConstraintDefaults.CurrentParametersSchemaVersion)
        {
            if (!Steps.TryGetValue(version, out var step))
            {
                errors.Add($"No upgrade step exists for '{PlanningConstraintDefaults.SchemaVersionProperty}' {version}.");
                return false;
            }

            upgraded = step(upgraded);
            version++;
            upgraded[PlanningConstraintDefaults.SchemaVersionProperty] = version;
        }

        return true;
    }

    private static bool TryReadVersion(JsonObject parameters, out int version)
    {
        version = 0;
        return parameters[PlanningConstraintDefaults.SchemaVersionProperty] is JsonValue value
            && value.TryGetValue(out version);
    }
}
