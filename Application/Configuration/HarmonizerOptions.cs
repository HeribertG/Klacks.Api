// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Configuration for the Wizard 2 harmonizer engine. The benchmark of 2026-06-29 showed that a single
/// deterministic conductor pass beats the genetic loop on plan quality per second, so the conductor-only
/// path is the default and the GA is opt-in for comparison runs.
/// </summary>
/// <param name="UseEvolution">True runs the full genetic loop; false (default) runs a single conductor pass alongside the untouched seed</param>
/// <param name="HarmonyGuard">True (default) lets no move take the global harmony between the rows (rhythm and fairness)
/// below the harmony of the start plan; false is the plain harmonizer that only looks at each row on its own</param>
namespace Klacks.Api.Application.Configuration;

public class HarmonizerOptions
{
    public const string SectionName = "Harmonizer";

    public bool UseEvolution { get; set; }

    public bool HarmonyGuard { get; set; } = true;
}
