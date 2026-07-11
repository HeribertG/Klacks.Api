// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Shared identifiers for the Holistic Harmonizer model eval goldset. The runner persists
/// EvalRun rows under this goldset name and the model check service reads the latest run
/// per model from the same goldset to rank models by measured proposal quality.
/// </summary>
public static class HarmonizerEvalGoldset
{
    public const string Name = "harmonizer-v1";
}
