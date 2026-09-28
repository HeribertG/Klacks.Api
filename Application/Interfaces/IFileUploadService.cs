// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public interface IFileUploadService
{
    Task StoreFileAsync(IFormFile file);
}
