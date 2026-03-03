// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Email;

public record GetEmailsByGroupQuery(Guid GroupId, int Skip, int Take) : IRequest<ReceivedEmailListResponse>;
