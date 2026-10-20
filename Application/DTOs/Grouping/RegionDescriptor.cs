// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// A region of a country's region file as GroupPartitionPlanner needs it.
/// </summary>
/// <param name="Key">Language-independent region key from the region file; part of the planning key so a language change never changes a region's identity.</param>
/// <param name="DisplayName">Name a newly created region root gets, in the installation's language.</param>
/// <param name="AllNames">Every language variant of the region name; an existing root group carrying ANY of them is reused.</param>
public sealed record RegionDescriptor(string Key, string DisplayName, IReadOnlyCollection<string> AllNames);
