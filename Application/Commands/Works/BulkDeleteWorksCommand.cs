// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes several Works. ExpectedChildren is set only by callers that confirmed a preview (the bulk delete
/// skill): the handler then refuses, before anything is staged, when the live container children of the Works it is about
/// to delete are not exactly those the user saw.
/// </summary>
/// <param name="Request">The ids of the Works to delete</param>
/// <param name="ExpectedChildren">The container children (child Works and Breaks) the user confirmed, or null for no check</param>

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Commands.Works;

public record BulkDeleteWorksCommand(BulkDeleteWorksRequest Request, ContainerChildIds? ExpectedChildren = null)
    : IRequest<BulkWorksResponse>;
