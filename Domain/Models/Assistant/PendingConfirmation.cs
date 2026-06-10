// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record PendingConfirmation(
    Guid UserId,
    string SkillName,
    IReadOnlyDictionary<string, object> Parameters,
    DateTime ExpiresAtUtc);
