// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fingerprint of everything the turn-intent hand features are computed from: TurnIntentHandFeatures,
/// MutationIntentDetector and every language plugin's mutation-intent.json. A trained head records it as
/// FeatureSourceHash, so a change to the detector or a plugin keyword list - which silently changes the feature columns
/// the head sees at runtime - fails the guard test until the head is retrained. Line endings are normalised so a
/// checkout with CRLF and one with LF give the same value.
/// </summary>
/// <param name="apiDirectory">The Klacks.Api source directory</param>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Application.Klacksy;

public static class TurnIntentFeatureSourceFingerprint
{
    private const string PluginsDirectory = "Plugins/Languages";
    private const string MutationIntentFileName = "mutation-intent.json";
    private const string CarriageReturnLineFeed = "\r\n";
    private const string LineFeed = "\n";
    private const char PathSeparator = '/';

    private static readonly string[] CodeFiles =
    [
        "Domain/Services/Assistant/MutationIntentDetector.cs",
        "Domain/Services/Assistant/TurnIntentHandFeatures.cs",
    ];

    public static string Compute(string apiDirectory)
    {
        var pluginFiles = Directory
            .EnumerateFiles(Path.Combine(apiDirectory, PluginsDirectory), MutationIntentFileName, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(apiDirectory, path).Replace(Path.DirectorySeparatorChar, PathSeparator));

        var builder = new StringBuilder();
        foreach (var relativePath in CodeFiles.Concat(pluginFiles).OrderBy(path => path, StringComparer.Ordinal))
        {
            var content = File.ReadAllText(Path.Combine(apiDirectory, relativePath))
                .Replace(CarriageReturnLineFeed, LineFeed, StringComparison.Ordinal);
            builder.Append(relativePath).Append(LineFeed).Append(content).Append(LineFeed);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLower(CultureInfo.InvariantCulture);
    }
}
