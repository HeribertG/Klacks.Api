// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Remembers per model whether it can actually read the schedule bitmap image. The capability check is a
/// full PNG round-trip that costs up to 90 seconds, so it must not run on every job. Negative verdicts
/// expire quickly so a freshly repaired API key is not locked out for hours.
/// </summary>
public sealed class HolisticHarmonizerModelCapabilityCache
{
    private const int PositiveTtlHours = 6;
    private const int NegativeTtlMinutes = 10;

    private readonly ConcurrentDictionary<string, CachedVerdict> _verdicts = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns a cached verdict for the model, or false when nothing valid is cached.
    /// </summary>
    /// <param name="modelId">Model to look up.</param>
    /// <param name="isVisionCapable">Cached verdict; only meaningful when the method returns true.</param>
    /// <param name="error">Reason recorded with a negative verdict; null for a positive one.</param>
    public bool TryGet(string modelId, out bool isVisionCapable, out string? error)
    {
        isVisionCapable = false;
        error = null;

        if (!_verdicts.TryGetValue(modelId, out var cached))
        {
            return false;
        }

        if (cached.ExpiresAt <= DateTime.UtcNow)
        {
            _verdicts.TryRemove(modelId, out _);
            return false;
        }

        isVisionCapable = cached.IsVisionCapable;
        error = cached.Error;
        return true;
    }

    /// <summary>
    /// Stores a verdict for the model. Positive verdicts live for hours, negative ones for minutes.
    /// </summary>
    /// <param name="modelId">Model the verdict belongs to.</param>
    /// <param name="isVisionCapable">True when the model answered the image round-trip correctly.</param>
    /// <param name="error">Reason for a negative verdict; ignored when the verdict is positive.</param>
    public void Store(string modelId, bool isVisionCapable, string? error)
    {
        var ttl = isVisionCapable
            ? TimeSpan.FromHours(PositiveTtlHours)
            : TimeSpan.FromMinutes(NegativeTtlMinutes);

        _verdicts[modelId] = new CachedVerdict(isVisionCapable, error, DateTime.UtcNow.Add(ttl));
    }

    private sealed record CachedVerdict(bool IsVisionCapable, string? Error, DateTime ExpiresAt);
}
