// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Accounts;

public interface IAccountRegistrationService
{
    Task<AuthenticatedResult> RegisterUserAsync(AppUser user, string password);
}