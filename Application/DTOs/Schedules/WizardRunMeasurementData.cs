// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The database facts the post-apply measurement needs for one capture, loaded as a unit so the churn
/// calculation stays pure. <see cref="ProposalCells"/> are the created works (loaded with soft-deleted rows
/// visible); <see cref="EventCells"/> are the (agent, date) cells carrying a post-apply Break or recovery
/// marker, which reclassify a change from correction to event churn.
/// </summary>
public sealed record WizardRunMeasurementData(
    IReadOnlyList<WizardRunProposalCell> ProposalCells,
    IReadOnlySet<(Guid AgentId, DateOnly Date)> EventCells);
