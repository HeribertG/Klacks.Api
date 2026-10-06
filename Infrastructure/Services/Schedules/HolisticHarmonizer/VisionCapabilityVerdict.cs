// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Outcome of evaluating one vision capability answer.
/// </summary>
/// <param name="Outcome">Passed = token read back; Misread = the model answered without the token; Inconclusive = the answer says nothing about vision</param>
/// <param name="Error">Why the read did not pass; null when passed</param>
public sealed record VisionCapabilityVerdict(VisionCapabilityOutcome Outcome, string? Error);
