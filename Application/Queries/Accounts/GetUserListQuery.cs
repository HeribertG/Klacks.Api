// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;
using System.Collections.Generic;

namespace Klacks.Api.Application.Queries.Accounts;

public record GetUserListQuery() : IRequest<List<UserResource>>;