// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class TranslatedEmailResource
{
    public string Subject { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public string? BodyText { get; set; }

    public string TargetLanguage { get; set; } = string.Empty;
}
