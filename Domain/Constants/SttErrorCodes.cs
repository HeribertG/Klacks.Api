// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Machine codes of speech-recognition rejections shared by the self-hosted engine's error body and the coded
/// rejections the API writes.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SttErrorCodes
{
    public const string UnsupportedLanguage = "unsupported_language";
}
