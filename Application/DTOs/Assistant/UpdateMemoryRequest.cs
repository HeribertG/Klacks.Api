// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public record UpdateMemoryRequest(string? Key, string? Content, string? Category, int? Importance, bool? IsPinned);
