// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public record MemorySearchResult(Guid Id, string Content, string Key, string Category, int Importance, float Score, bool IsPinned);
