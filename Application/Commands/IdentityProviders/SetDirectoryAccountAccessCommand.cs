// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.IdentityProviders;

/// <summary>
/// Grants or withdraws sign-in for one directory account of an LDAP/AD provider.
/// </summary>
/// <param name="ProviderId">Identity provider id</param>
/// <param name="ExternalId">Stable directory id of the account (LdapUserEntry.StableId)</param>
/// <param name="MayLogin">True grants (creates, links or reactivates the account), false deactivates the linked account</param>
public record SetDirectoryAccountAccessCommand(Guid ProviderId, string ExternalId, bool MayLogin) : IRequest<DirectoryAccountResource>;
