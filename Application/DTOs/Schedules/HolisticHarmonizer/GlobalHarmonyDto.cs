// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <summary>
/// Global harmony of a plan: how well the rows fit together (second instance of the harmonizer), each value 0..1.
/// </summary>
/// <param name="Total">Weighted sum of rhythm and fairness.</param>
/// <param name="Rhythm">How well the rows are shifted copies of one common pattern (canon).</param>
/// <param name="Fairness">How evenly early, late, night and weekend work is shared relative to each row's own working days.</param>
public sealed record GlobalHarmonyDto(double Total, double Rhythm, double Fairness);
