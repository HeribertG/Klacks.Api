// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// One child of a container as the split sees it: a service (task) or an absence with its start and end.
/// </summary>
/// <param name="Id">Id of the sub-work or sub-break</param>
/// <param name="Start">Start time of the child</param>
/// <param name="End">End time of the child (before Start when it crosses midnight)</param>
/// <param name="IsTask">True for a sub-work, false for a sub-break</param>
public sealed record ContainerSplitItem(Guid Id, TimeOnly Start, TimeOnly End, bool IsTask);
