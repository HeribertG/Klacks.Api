// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IFileUploadService
{
    Task StoreFileAsync(IFormFile file);
}
