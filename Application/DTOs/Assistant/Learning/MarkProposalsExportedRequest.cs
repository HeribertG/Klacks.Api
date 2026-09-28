// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Body of POST learning/mark-exported.
/// </summary>
/// <param name="ProposalIds">Gate-passed proposals the export script wrote into skill-seeds.json</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record MarkProposalsExportedRequest(IReadOnlyList<Guid> ProposalIds);
