// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Assistant;

public sealed record ClientCoreDataStatus(
    Guid ClientId,
    string? FirstName,
    string Name,
    bool HasActiveAddress,
    bool HasEmailOrPhone);
