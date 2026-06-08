// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the phonetic encoder named in a language's PhoneticConfig.
/// "koelner" → Cologne phonetics (German); anything else → the data-driven
/// rule-based encoder built from the config's rules.
/// </summary>
namespace Klacks.Api.Domain.Services.Assistant.Phonetics;

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class PhoneticEncoderFactory : IPhoneticEncoderFactory
{
    public const string Koelner = "koelner";
    public const string RuleBased = "rule_based";

    private static readonly KoelnerPhoneticEncoder KoelnerEncoder = new();

    public IPhoneticEncoder Create(PhoneticConfig config)
    {
        return config.Encoder == Koelner
            ? KoelnerEncoder
            : new RuleBasedPhoneticEncoder(config.Rules);
    }
}
