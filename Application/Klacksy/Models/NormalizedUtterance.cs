// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Output of UtteranceNormalizer. Keeps both original and normalized for telemetry vs matching.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public sealed record NormalizedUtterance(
    string Original,
    string Normalized,
    bool WakeWordStripped,
    bool IsEmptyAfterNormalization);
