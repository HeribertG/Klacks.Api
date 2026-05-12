// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILanguageMetadataProvider
{
    string? GetName(string langCode);
    string? GetDisplayName(string langCode);
}
