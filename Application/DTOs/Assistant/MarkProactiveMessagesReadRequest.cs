// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class MarkProactiveMessagesReadRequest
{
    public IReadOnlyList<Guid> Ids { get; set; } = [];
}
