// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Discriminator values of LearnedPhraseDto.Source. The admin card echoes the value back on a PUT, and
/// the handler uses it to decide which store the id addresses.
/// </summary>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public static class LearnedPhraseSources
{
    public const string Learned = "learned";
    public const string Description = "description";
}
