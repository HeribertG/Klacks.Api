// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.ClientImport;

public interface IClientImportTemplateBuilder
{
    byte[] Build(string sheetName, IReadOnlyList<string> headers);
}
