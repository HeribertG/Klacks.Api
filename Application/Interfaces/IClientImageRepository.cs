// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientImageRepository
{
    Task<ClientImage?> GetByClientIdAsync(Guid clientId);
    Task<bool> DeleteByClientIdAsync(Guid clientId);
    Task Add(ClientImage clientImage);
    Task Delete(Guid id);
}
