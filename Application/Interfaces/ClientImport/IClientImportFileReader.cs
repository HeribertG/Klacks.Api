// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads an uploaded employee list into a raw string grid. Implementations reject anything they cannot
/// read safely with a ClientImportRejectedException carrying a ClientImportErrorCodes code.
/// </summary>

using Klacks.Api.Application.Services.ClientImport;

namespace Klacks.Api.Application.Interfaces.ClientImport;

public interface IClientImportFileReader
{
    bool CanRead(string fileName);

    ClientImportSheet Read(byte[] content, string? sheetName);
}
