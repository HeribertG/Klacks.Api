// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public record CreateAgentRequest(string Name, string? DisplayName, string? Description);
