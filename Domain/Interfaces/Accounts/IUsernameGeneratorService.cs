// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Accounts;

public interface IUsernameGeneratorService
{
    Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName);
}
