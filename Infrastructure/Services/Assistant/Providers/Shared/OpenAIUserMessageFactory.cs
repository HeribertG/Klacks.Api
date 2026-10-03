// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the final user message of an OpenAI-style chat/completions request. Without an image the content stays
/// a plain string, because some OpenAI-compatible backends reject content arrays for text-only models; with an
/// image it becomes a text block plus an image_url block carrying the PNG as an inline data URI. Shared by every
/// provider that speaks the OpenAI message format so none of them can silently drop an attached image again.
/// </summary>
/// <param name="message">Text of the user turn</param>
/// <param name="imagePng">Optional PNG attached to the user turn; null or empty means a text-only message</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public static class OpenAIUserMessageFactory
{
    public const string UserRole = "user";

    public const string PngDataUriPrefix = "data:image/png;base64,";

    public static OpenAIMessage Create(string message, byte[]? imagePng)
    {
        if (imagePng is not { Length: > 0 })
        {
            return new OpenAIMessage { Role = UserRole, Content = message };
        }

        return new OpenAIMessage
        {
            Role = UserRole,
            Content = new object[]
            {
                new OpenAITextContent(message),
                new OpenAIImageContent(new OpenAIImageUrl(PngDataUriPrefix + Convert.ToBase64String(imagePng))),
            }
        };
    }
}
