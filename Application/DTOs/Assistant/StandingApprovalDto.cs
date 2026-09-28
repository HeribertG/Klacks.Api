// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public record StandingApprovalDto(
    Guid Id,
    string TriggerKind,
    Guid? GroupId,
    Guid GrantedByUserId,
    DateTime GrantedAtUtc,
    DateTime ExpiresAtUtc,
    int DailyBudget,
    DateTime? RevokedAtUtc,
    Guid? RevokedByUserId,
    bool IsActive);
