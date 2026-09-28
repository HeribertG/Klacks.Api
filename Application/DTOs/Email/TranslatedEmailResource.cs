// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

public class TranslatedEmailResource
{
    public string Subject { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public string? BodyText { get; set; }

    public string TargetLanguage { get; set; } = string.Empty;
}
