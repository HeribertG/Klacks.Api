// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces;

public interface IUserService
{
    Guid? GetId();

    string? GetIdString();

    string GetUserName();

    Task<bool> IsAdmin();
}
