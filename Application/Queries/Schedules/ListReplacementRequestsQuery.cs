// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Lists replacement request rows; a row is only returned when its candidate is visible to the caller (admins
/// see all). Each row carries the short-notice verdict computed with the current threshold setting.
/// </summary>
/// <param name="AbsentClientId">Optional absent employee</param>
/// <param name="FromDate">Optional first slot date (inclusive)</param>
/// <param name="UntilDate">Optional last slot date (inclusive)</param>
/// <param name="AnalyseToken">Optional scenario token</param>
public record ListReplacementRequestsQuery(
    Guid? AbsentClientId,
    DateOnly? FromDate,
    DateOnly? UntilDate,
    Guid? AnalyseToken) : IRequest<IReadOnlyList<ReplacementRequestResource>>;
