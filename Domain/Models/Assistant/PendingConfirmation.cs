// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record PendingConfirmation(
    Guid UserId,
    string SkillName,
    IReadOnlyDictionary<string, object> Parameters,
    DateTime ExpiresAtUtc);
