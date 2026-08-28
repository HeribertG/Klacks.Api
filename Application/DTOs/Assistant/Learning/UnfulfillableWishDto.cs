// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A wish people repeated often enough to matter that the assistant still cannot serve. Carries an
/// excerpt of at most 120 characters and never a user id - the row is evidence about the product.
/// </summary>
/// <param name="LastError">Why the last learning attempt failed, null before stage G2 attempts any</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record UnfulfillableWishDto(
    Guid Id,
    string IntentExcerpt,
    string Locale,
    string Status,
    int OccurrenceCount,
    int DistinctUserCount,
    DateTime FirstSeen,
    DateTime LastSeen,
    string? LastError);
