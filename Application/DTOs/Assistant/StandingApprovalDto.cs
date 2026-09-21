// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
