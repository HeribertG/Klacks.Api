// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IExportFormatFamilyResolver
{
    IReadOnlyList<(string FormatKey, string Family)> GetAll();

    bool TryResolve(string formatKey, out string family);
}
