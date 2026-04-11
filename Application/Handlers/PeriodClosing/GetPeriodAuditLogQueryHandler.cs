// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for loading period audit log entries enriched with group display names.
/// </summary>
/// <param name="request">Contains From and To date range for filtering audit entries</param>

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

public class GetPeriodAuditLogQueryHandler : BaseHandler, IRequestHandler<GetPeriodAuditLogQuery, List<PeriodAuditLogDto>>
{
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly DataBaseContext _context;

    public GetPeriodAuditLogQueryHandler(
        IPeriodAuditLogRepository auditLogRepository,
        DataBaseContext context,
        ILogger<GetPeriodAuditLogQueryHandler> logger)
        : base(logger)
    {
        _auditLogRepository = auditLogRepository;
        _context = context;
    }

    public async Task<List<PeriodAuditLogDto>> Handle(GetPeriodAuditLogQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entries = await _auditLogRepository.GetRangeAsync(request.From, request.To, cancellationToken);

            var groupIds = entries
                .Where(e => e.GroupId.HasValue)
                .Select(e => e.GroupId!.Value)
                .Distinct()
                .ToList();

            var groupNames = groupIds.Count > 0
                ? await _context.Group
                    .AsNoTracking()
                    .Where(g => groupIds.Contains(g.Id))
                    .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken)
                : new Dictionary<Guid, string>();

            return entries.Select(e => new PeriodAuditLogDto
            {
                Id = e.Id,
                Action = e.Action,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                GroupId = e.GroupId,
                GroupName = e.GroupId.HasValue && groupNames.TryGetValue(e.GroupId.Value, out var name) ? name : null,
                Reason = e.Reason,
                AffectedCount = e.AffectedCount,
                PerformedAt = e.PerformedAt,
                PerformedBy = e.PerformedBy
            }).ToList();
        },
        "loading period audit log",
        new { request.From, request.To });
    }
}
