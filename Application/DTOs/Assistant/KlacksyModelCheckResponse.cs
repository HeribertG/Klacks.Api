// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

/// <param name="Models">Probed models sorted best-first, including their applied enabled/default state.</param>
/// <param name="DefaultModelId">The resulting default model id; null when no model qualified.</param>
public sealed record KlacksyModelCheckResponse(
    KlacksyModelCheckDto[] Models,
    string? DefaultModelId);
