// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.IdentityProviders;

/// <summary>
/// Reads the directory approval list of one LDAP/AD provider.
/// </summary>
/// <param name="ProviderId">Identity provider id</param>
/// <param name="Search">Optional substring the directory search filters on</param>
public record ListDirectoryAccountsQuery(Guid ProviderId, string? Search) : IRequest<DirectoryAccountListResource>;
