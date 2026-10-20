// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.ClientImport;

public interface IClientImportTemplateBuilder
{
    /// <param name="sheetName">Name of the only sheet</param>
    /// <param name="headers">Header text per column</param>
    /// <param name="headerNotes">Optional note per column shown on its header cell (null entries for none)</param>
    byte[] Build(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<string?>? headerNotes = null);
}
