// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILanguageMetadataProvider
{
    string? GetName(string langCode);
    string? GetDisplayName(string langCode);
}
