// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Setting values of <c>WIZARD3_MODE</c> and their parsing. A missing, blank or unknown value selects the
/// deterministic optimizer, so stage 3 runs without any LLM or API key unless the LLM path is chosen explicitly.
/// </summary>
public static class HolisticHarmonizerModes
{
    public const string Deterministic = "deterministic";
    public const string Llm = "llm";

    public static HolisticHarmonizerMode Parse(string? value)
        => string.Equals(value?.Trim(), Llm, StringComparison.OrdinalIgnoreCase)
            ? HolisticHarmonizerMode.Llm
            : HolisticHarmonizerMode.Deterministic;

    public static bool IsKnown(string? value)
        => string.IsNullOrWhiteSpace(value)
           || string.Equals(value.Trim(), Deterministic, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value.Trim(), Llm, StringComparison.OrdinalIgnoreCase);
}
