// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the format-specific file name of a formatter when it provides one.
/// </summary>
/// <param name="formatter">The formatter that produces the file content</param>
/// <param name="baseName">The standard file name including the extension</param>
using Klacks.Api.Domain.Interfaces.Exports;

namespace Klacks.Api.Application.Services.Exports;

public static class ExportFileNameDecorator
{
    public static string Apply(IExportFormatter formatter, string baseName)
    {
        return formatter is IExportFileNameProvider provider ? provider.DecorateFileName(baseName) : baseName;
    }
}
