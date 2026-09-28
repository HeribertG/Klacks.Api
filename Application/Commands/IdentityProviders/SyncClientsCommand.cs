// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Commands.IdentityProviders;

public record SyncClientsCommand(Guid Id) : IRequest<IdentityProviderSyncResultResource>;
