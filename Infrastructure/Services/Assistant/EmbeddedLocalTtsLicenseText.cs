// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the licence text of the local voice model (Infrastructure/Resources/openrail-m-supertonic3.txt, embedded under
/// LocalTtsConstants.LicenseResourceName) once from the assembly manifest.
/// </summary>
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class EmbeddedLocalTtsLicenseText : ILocalTtsLicenseText
{
    private readonly Lazy<string> _text = new(Load);

    public string Text => _text.Value;

    private static string Load()
    {
        using var stream = typeof(EmbeddedLocalTtsLicenseText).Assembly.GetManifestResourceStream(LocalTtsConstants.LicenseResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{LocalTtsConstants.LicenseResourceName}' is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
