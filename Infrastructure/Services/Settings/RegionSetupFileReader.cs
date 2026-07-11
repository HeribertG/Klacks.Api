// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared reader for the optional region setup profile file: resolves the configured path,
/// reads the raw content and parses it into a strongly typed profile with fail-fast semantics.
/// </summary>
/// <param name="configuration">App configuration providing the RegionSetup:File path</param>
/// <param name="filePath">Absolute or relative path to the region setup JSON file</param>
/// <param name="content">Raw JSON content of the region setup file</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings;

public static class RegionSetupFileReader
{
    public const string FileConfigKey = "RegionSetup:File";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? GetConfiguredPath(IConfiguration configuration)
    {
        var filePath = configuration[FileConfigKey];
        return string.IsNullOrWhiteSpace(filePath) ? null : filePath;
    }

    public static async Task<string> ReadContentAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Region setup file '{filePath}' is configured but does not exist.", filePath);
        }

        return await File.ReadAllTextAsync(filePath);
    }

    public static RegionSetupProfile Parse(string content, string filePath)
    {
        RegionSetupProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<RegionSetupProfile>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidRequestException($"Region setup file '{filePath}' is not a valid region setup profile: {ex.Message}");
        }

        if (profile == null)
        {
            throw new InvalidRequestException($"Region setup file '{filePath}' is empty or contains no JSON object.");
        }

        return profile;
    }

    public static async Task<RegionSetupProfile> ReadProfileAsync(string filePath)
    {
        var content = await ReadContentAsync(filePath);
        return Parse(content, filePath);
    }
}
