// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IExportFormatFamilyResolver
{
    IReadOnlyList<(string FormatKey, string Family)> GetAll();

    bool TryResolve(string formatKey, out string family);
}
