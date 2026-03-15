// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public record MemorySearchResult(Guid Id, string Content, string Key, string Category, int Importance, float Score, bool IsPinned);
