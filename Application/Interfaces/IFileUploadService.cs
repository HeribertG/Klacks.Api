// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.FileHandling;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Stores, reads and deletes the images of the LoadFile endpoints under their validated server-side names.
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Writes the content to target.FileName (overwriting) and removes the slot's other stored variants.
    /// </summary>
    Task StoreImageAsync(ImageFileTarget target, Stream content, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the first existing stored variant of the target, or null when none exists.
    /// </summary>
    Task<byte[]?> ReadImageAsync(ImageFileTarget target, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes every stored variant of the target.
    /// </summary>
    void DeleteImage(ImageFileTarget target);
}
