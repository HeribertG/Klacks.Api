// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IFileUploadService
{
    void StoreFile(IFormFile file);
}
