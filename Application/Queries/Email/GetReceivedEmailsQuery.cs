// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Email;

public record GetReceivedEmailsQuery(
    int Skip, int Take,
    string? Folder = null,
    string? ReadFilter = null,
    string? SortDirection = null
) : IRequest<ReceivedEmailListResponse>;
