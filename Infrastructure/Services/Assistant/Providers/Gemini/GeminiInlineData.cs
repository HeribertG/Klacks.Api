// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;

/// <summary>
/// Gemini multimodal payload for inline binary content. Attached to a <see cref="GeminiPart"/>
/// when the same part should ship base64-encoded image bytes alongside the text turn. The
/// Gemini API accepts <c>inline_data</c> with the explicit MIME type so it does not need to
/// sniff bytes.
/// </summary>
/// <param name="MimeType">IANA media type, e.g. <c>image/png</c>.</param>
/// <param name="Data">Base64-encoded payload (no <c>data:</c> URI prefix).</param>
public sealed record GeminiInlineData(
    [property: JsonPropertyName("mime_type")] string MimeType,
    [property: JsonPropertyName("data")] string Data);
