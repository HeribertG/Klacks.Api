// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Optional capability of an export formatter whose target system requires a format-specific file name.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Exports;

public interface IExportFileNameProvider
{
    string DecorateFileName(string baseName);
}
