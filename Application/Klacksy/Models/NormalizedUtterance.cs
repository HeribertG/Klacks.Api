// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Output of UtteranceNormalizer. Keeps both original and normalized for telemetry vs matching.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public sealed record NormalizedUtterance(
    string Original,
    string Normalized,
    bool WakeWordStripped,
    bool IsEmptyAfterNormalization);
