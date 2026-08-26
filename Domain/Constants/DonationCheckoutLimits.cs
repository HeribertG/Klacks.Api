// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of truth for the boundaries of a donation checkout: the currencies the Stripe
/// session may be created in and the amount range it accepts. Shared by
/// CreateDonationCheckoutCommandHandler, which builds the Stripe session, and by the
/// create_donation_checkout skill, which rejects an impossible request in the chat before it ever
/// reaches the endpoint. Keeping both on the same constants is what stops the skill from advertising
/// a currency the handler would refuse.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class DonationCheckoutLimits
{
    public const string CurrencyChf = "CHF";

    public const string CurrencyEur = "EUR";

    /// <summary>
    /// Currency used when a caller names none. Mirrors the donation dialog in the Klacks.Ui footer,
    /// which also opens on CHF.
    /// </summary>
    public const string DefaultCurrency = CurrencyChf;

    public const decimal MinAmount = 1m;

    public const decimal MaxAmount = 10000m;

    public static readonly IReadOnlyList<string> SupportedCurrencies = [CurrencyChf, CurrencyEur];
}
