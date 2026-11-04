// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A validated, server-side image file name of the LoadFile endpoints.
/// </summary>
/// <param name="Slot">Which kind of image the name addresses (logo, icon, profile picture, report image)</param>
/// <param name="OwnerUserId">The user a profile picture belongs to; null for every other slot</param>
/// <param name="FileName">The canonical file name written on upload</param>
/// <param name="StoredFileNames">Every file name the slot may occupy on disk, in download preference order</param>
/// <param name="DeclaredFormat">The format implied by the requested extension when the client chose it (profile pictures); null for fixed names</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.FileHandling;

public sealed record ImageFileTarget(
    ImageFileSlot Slot,
    Guid? OwnerUserId,
    string FileName,
    IReadOnlyList<string> StoredFileNames,
    ImageFileFormat? DeclaredFormat);
