// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Handler for loading export log entries enriched with group display names.
/// </summary>
public class GetExportLogQueryHandler : BaseHandler, IRequestHandler<GetExportLogQuery, List<ExportLogDto>>
{
    private readonly IExportLogRepository _exportLogRepository;
    private readonly DataBaseContext _context;

    public GetExportLogQueryHandler(
        IExportLogRepository exportLogRepository,
        DataBaseContext context,
        ILogger<GetExportLogQueryHandler> logger)
        : base(logger)
    {
        _exportLogRepository = exportLogRepository;
        _context = context;
    }

    public async Task<List<ExportLogDto>> Handle(GetExportLogQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entries = await _exportLogRepository.GetRangeAsync(request.From, request.To, cancellationToken);

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

            var userIds = entries
                .Select(e => e.ExportedBy)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var userNames = userIds.Count > 0
                ? await _context.AppUser
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken)
                : new Dictionary<string, string>();

            return entries.Select(e => new ExportLogDto
            {
                Id = e.Id,
                Format = e.Format,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                GroupId = e.GroupId,
                GroupName = e.GroupId.HasValue && groupNames.TryGetValue(e.GroupId.Value, out var name) ? name : null,
                Language = e.Language,
                CurrencyCode = e.CurrencyCode,
                FileName = e.FileName,
                FileSize = e.FileSize,
                RecordCount = e.RecordCount,
                ExportedAt = e.ExportedAt,
                ExportedBy = e.ExportedBy,
                ExportedByName = userNames.TryGetValue(e.ExportedBy, out var userName) && !string.IsNullOrWhiteSpace(userName)
                    ? userName
                    : null
            }).ToList();
        },
        "loading export log",
        new { request.From, request.To });
    }
}
