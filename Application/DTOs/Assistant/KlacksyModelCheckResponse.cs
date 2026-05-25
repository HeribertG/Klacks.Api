// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <param name="Models">Probed models sorted best-first, including their applied enabled/default state.</param>
/// <param name="DefaultModelId">The resulting default model id; null when no model qualified.</param>
public sealed record KlacksyModelCheckResponse(
    KlacksyModelCheckDto[] Models,
    string? DefaultModelId);
