// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration for the Wizard 2 harmonizer engine. The benchmark of 2026-06-29 showed that a single
/// deterministic conductor pass beats the genetic loop on plan quality per second, so the conductor-only
/// path is the default and the GA is opt-in for comparison runs.
/// </summary>
/// <param name="UseEvolution">True runs the full genetic loop; false (default) runs a single conductor pass alongside the untouched seed</param>
namespace Klacks.Api.Application.Configuration;

public class HarmonizerOptions
{
    public const string SectionName = "Harmonizer";

    public bool UseEvolution { get; set; }
}
