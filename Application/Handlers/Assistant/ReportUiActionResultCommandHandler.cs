// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a UiAction outcome report to the usage row the dispatch booked under the tracking id.
/// Only the user the skill ran for may report the outcome - the random Guid is the capability, not
/// the authorization. Unknown ids are not errors: the usage row may have been lost to a restart or
/// the client may double-report, and a 404 would teach the client nothing useful. A report against a
/// non-UiAction row or with an unknown status is rejected as a bad request, because that is a
/// programming error the client should see.
/// </summary>
/// <param name="repository">Usage store, self-committing</param>
/// <param name="logger">Reports misses, which are the only interesting outcome</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ReportUiActionResultCommandHandler
    : IRequestHandler<ReportUiActionResultCommand, ReportUiActionResultResult>
{
    private readonly ISkillUsageRepository _repository;
    private readonly ILogger<ReportUiActionResultCommandHandler> _logger;

    public ReportUiActionResultCommandHandler(
        ISkillUsageRepository repository,
        ILogger<ReportUiActionResultCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ReportUiActionResultResult> Handle(
        ReportUiActionResultCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        var status = ParseStatus(request.Status);
        if (status == null)
        {
            throw new ArgumentException(
                $"Status must be 'completed' or 'failed' but was '{request.Status}'.", nameof(request));
        }

        var record = await _repository.GetByIdAsync(request.TrackingId, cancellationToken);
        var matchesCaller = Guid.TryParse(request.UserId, out var userId)
            && record != null
            && record.UserId == userId;
        if (!matchesCaller)
        {
            _logger.LogInformation(
                "UiAction report for {TrackingId} found no row of caller {UserId}; unknown or foreign id",
                request.TrackingId, request.UserId);
            return new ReportUiActionResultResult(Found: false, Updated: false, Error: null);
        }

        if (record.UiActionStatus == null)
        {
            throw new ArgumentException(
                $"Usage row {request.TrackingId} is not a UiAction dispatch.", nameof(request));
        }

        record.UiActionStatus = status.Value;
        record.Success = status.Value == UiActionStatus.Completed;
        record.ErrorMessage = status.Value == UiActionStatus.Failed
            ? Truncate(request.ErrorMessage) ?? "UiAction failed"
            : null;
        record.UpdateTime = DateTime.UtcNow;
        await _repository.UpdateAsync(record, cancellationToken);

        _logger.LogInformation(
            "UiAction {TrackingId} ({SkillName}) reported as {Status}",
            request.TrackingId, record.SkillName, status.Value);

        return new ReportUiActionResultResult(Found: true, Updated: true, Error: null);
    }

    private static UiActionStatus? ParseStatus(string status) =>
        string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)
            ? UiActionStatus.Completed
            : string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
                ? UiActionStatus.Failed
                : null;

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }
}
