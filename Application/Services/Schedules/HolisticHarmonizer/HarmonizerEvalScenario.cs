// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// One deterministic eval scenario for the Holistic Harmonizer model eval.
/// </summary>
/// <param name="Name">Stable scenario label used in dimensions and diagnostics.</param>
/// <param name="Input">Fully in-memory bitmap input (agents + assignments); never touches the database.</param>
public sealed record HarmonizerEvalScenario(string Name, BitmapInput Input);
