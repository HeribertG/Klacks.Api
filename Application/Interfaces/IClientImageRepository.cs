// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientImageRepository
{
    Task<ClientImage?> GetByClientIdAsync(Guid clientId);
    Task<bool> DeleteByClientIdAsync(Guid clientId);
    Task Add(ClientImage clientImage);
    Task Delete(Guid id);
}
