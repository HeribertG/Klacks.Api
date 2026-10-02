// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// A qualification that gets no group because fewer considered clients than the minimum hold it today.
/// </summary>
/// <param name="Name">Qualification name in the installation language.</param>
/// <param name="MemberCount">Considered clients holding it today.</param>
public sealed record SkippedQualificationGroup(string Name, int MemberCount);
