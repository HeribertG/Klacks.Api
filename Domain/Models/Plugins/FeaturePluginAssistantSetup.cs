// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Optional manifest section through which a feature plugin offers Klacksy's commissioning help after it
/// has been installed or switched on.
/// </summary>
/// <param name="OfferKey">Plugin i18n key of the offer text shown to the user</param>
/// <param name="TriggerPhraseKey">Plugin i18n key of the chat phrase that starts the commissioning help</param>
/// <param name="AcceptKey">Plugin i18n key of the button text that accepts the offer</param>
/// <param name="DeclineKey">Plugin i18n key of the button text that declines the offer</param>

namespace Klacks.Api.Domain.Models.Plugins;

public class FeaturePluginAssistantSetup
{
    public string OfferKey { get; set; } = string.Empty;
    public string TriggerPhraseKey { get; set; } = string.Empty;
    public string AcceptKey { get; set; } = string.Empty;
    public string DeclineKey { get; set; } = string.Empty;
}
