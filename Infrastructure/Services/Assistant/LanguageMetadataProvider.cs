// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class LanguageMetadataProvider : ILanguageMetadataProvider
{
    private readonly IConfiguration _configuration;

    public LanguageMetadataProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? GetName(string langCode) =>
        _configuration[$"Languages:Metadata:{langCode}:Name"];

    public string? GetDisplayName(string langCode) =>
        _configuration[$"Languages:Metadata:{langCode}:DisplayName"];
}
