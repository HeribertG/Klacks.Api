// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Planner-facing variant of the replacement search: runs <see cref="FindReplacementQuery"/> unchanged and adds
/// the eligible candidates' phone numbers. Kept apart from the skill path so phone numbers never reach LLM output.
/// </summary>
/// <param name="Search">The slot search exactly as find_replacement runs it</param>
public record GetReplacementCandidatesQuery(FindReplacementQuery Search) : IRequest<ReplacementSearchResult>;
