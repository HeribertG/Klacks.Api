// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;

namespace Klacks.Api.Application.Interfaces;

public interface IPeriodClosingReadRepository
{
    Task<Dictionary<Guid, string>> GetGroupNames(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken);

    Task<Dictionary<string, string>> GetUserDisplayNames(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);

    Task<List<PeriodScheduleNoteIssueRow>> GetScheduleNoteIssues(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? groupId,
        int maxNotes,
        CancellationToken cancellationToken);
}
