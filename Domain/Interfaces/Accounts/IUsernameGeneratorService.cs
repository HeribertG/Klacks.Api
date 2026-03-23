// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Accounts;

public interface IUsernameGeneratorService
{
    Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName);
}
