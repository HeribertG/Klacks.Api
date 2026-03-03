// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Interfaces;

public interface IPostcodeChRepository
{
    Task<List<PostcodeCH>> GetAllAsync();
    Task<List<PostcodeCH>> GetByZipAsync(int zip);
}
