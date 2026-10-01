// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportSynonymDocument
{
    public Dictionary<string, Dictionary<ClientImportTarget, List<string>>> Headers { get; set; } = [];

    public Dictionary<string, Dictionary<GenderEnum, List<string>>> GenderValues { get; set; } = [];
}
