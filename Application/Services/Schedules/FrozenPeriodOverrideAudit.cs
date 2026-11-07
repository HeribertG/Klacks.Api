// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the reason of the frozen-period override and writes its period audit row with the acting user, like the
/// reopen of a period does.
/// </summary>
/// <param name="auditLogRepository">Stages the audit row (stage-only)</param>
/// <param name="unitOfWork">Commits the staged audit row</param>
/// <param name="userService">Supplies the acting user's id claim and display name</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class FrozenPeriodOverrideAudit : IFrozenPeriodOverrideAudit
{
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;

    public FrozenPeriodOverrideAudit(
        IPeriodAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IUserService userService)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _userService = userService;
    }

    public void EnsureReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidRequestException("A reason must be provided when recalculating exported periods.");
        }

        if (reason.Trim().Length > PeriodAuditLimits.ReasonMaxLength)
        {
            throw new InvalidRequestException(
                $"The reason must not be longer than {PeriodAuditLimits.ReasonMaxLength} characters.");
        }
    }

    public async Task RecordAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId,
        string reason,
        int frozenClientCount,
        CancellationToken cancellationToken)
    {
        await _auditLogRepository.AddAsync(
            PeriodAuditLog.For(
                PeriodAuditAction.RecalculateExportedPeriod,
                startDate,
                endDate,
                groupId,
                reason.Trim(),
                frozenClientCount,
                _userService.GetIdString() ?? AuditActorDefaults.UnknownActor,
                _userService.GetDisplayName()),
            cancellationToken);
        await _unitOfWork.CompleteAsync();
    }
}
