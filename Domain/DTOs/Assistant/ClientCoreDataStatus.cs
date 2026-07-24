// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.Assistant;

public sealed record ClientCoreDataStatus(
    Guid ClientId,
    string? FirstName,
    string Name,
    bool HasActiveAddress,
    bool HasEmailOrPhone);
