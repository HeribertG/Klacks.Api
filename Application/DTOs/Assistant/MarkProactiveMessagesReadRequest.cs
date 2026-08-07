// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class MarkProactiveMessagesReadRequest
{
    public IReadOnlyList<Guid> Ids { get; set; } = [];
}
