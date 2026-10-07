// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IAbsenceRepository : IBaseRepository<Absence>
{
    HttpResultResource CreateExcelFile(string language);

    Task<TruncatedAbsence> Truncated(AbsenceFilter filter);

    Task<int> CountActiveBreaksByAbsenceAsync(Guid absenceId, CancellationToken cancellationToken = default);

    Task<int> CountActiveBreakPlaceholdersByAbsenceAsync(Guid absenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of every absence type flagged as on-call (<see cref="Absence.IsOnCall"/>), soft-deleted types
    /// included, so existing breaks of a retired on-call type keep their on-call meaning.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetOnCallAbsenceIdsAsync(CancellationToken cancellationToken = default);
}
