// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public record UpdateAgentRequest(string? Name, string? DisplayName, string? Description, bool? IsActive);
