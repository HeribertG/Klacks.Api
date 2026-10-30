// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Update;

/// <summary>
/// Asks whether installing the offered update now is a good idea.
/// </summary>
/// <param name="CallerId">User id of the asking administrator, left out of the count of other connected users</param>
public record EvaluateSoftwareUpdateQuery(string CallerId) : IRequest<SoftwareUpdateEvaluation>;
