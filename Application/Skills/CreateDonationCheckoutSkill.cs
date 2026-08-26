// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts a donation to the Klacks project by calling POST api/backend/Donation/checkout-session on the
/// own REST API under the caller's token, so [Authorize] and the request log apply exactly as they do to
/// the donation dialog in the footer. The endpoint creates a Stripe Checkout session and returns its
/// hosted payment URL; this skill only hands that URL back — nothing is charged until the user opens it
/// and completes the payment on Stripe. Amount and currency are checked against DonationCheckoutLimits,
/// the same constants the command handler validates against, so an impossible request fails in the chat
/// instead of costing a round trip. When Stripe is not configured the endpoint answers 400 and the
/// concrete cause is relayed unchanged.
/// </summary>
/// <param name="amount">Donation amount, between DonationCheckoutLimits.MinAmount and MaxAmount (required).</param>
/// <param name="currency">Currency of the donation: CHF or EUR; defaults to CHF when omitted.</param>

using Klacks.Api.Application.DTOs.Donations;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CreateDonationCheckoutSkill : BaseSkillImplementation
{
    private const string SkillName = "create_donation_checkout";
    private const string AmountParameter = "amount";
    private const string CurrencyParameter = "currency";

    private readonly IKlacksSelfApiClient _selfApi;

    public CreateDonationCheckoutSkill(IKlacksSelfApiClient selfApi)
    {
        _selfApi = selfApi;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var amount = GetParameter<decimal?>(parameters, AmountParameter)
            ?? throw new ArgumentException($"Required parameter '{AmountParameter}' is missing");

        if (amount < DonationCheckoutLimits.MinAmount || amount > DonationCheckoutLimits.MaxAmount)
        {
            return SkillResult.Error(
                $"A donation must be between {DonationCheckoutLimits.MinAmount} and " +
                $"{DonationCheckoutLimits.MaxAmount}; {amount} is outside that range.");
        }

        var requestedCurrency = GetParameter<string>(parameters, CurrencyParameter);
        var currency = ResolveCurrency(requestedCurrency);
        if (currency is null)
        {
            return SkillResult.Error(
                $"'{requestedCurrency}' is not a currency this donation checkout supports. Supported " +
                $"currencies: {string.Join(", ", DonationCheckoutLimits.SupportedCurrencies)}.");
        }

        var result = await _selfApi.PostAsync<CreateDonationCheckoutResponse>(
            SelfApiRoutes.DonationCheckoutSession,
            new CreateDonationCheckoutRequest { Amount = amount, Currency = currency },
            context,
            SkillName,
            cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        var checkoutUrl = result.Value?.Url;
        if (string.IsNullOrWhiteSpace(checkoutUrl))
        {
            return SkillResult.Error(
                "The donation checkout was accepted but came back without a payment link, so the donation " +
                "cannot be completed. Try again later or use the donation dialog in the footer.");
        }

        return SkillResult.SuccessResult(
            new
            {
                Amount = amount,
                Currency = currency,
                Url = checkoutUrl
            },
            $"A Stripe checkout page for a donation of {amount} {currency} is ready at {checkoutUrl}. " +
            "Open that link to complete the payment — nothing has been charged yet.");
    }

    private static string? ResolveCurrency(string? requestedCurrency)
    {
        if (string.IsNullOrWhiteSpace(requestedCurrency))
        {
            return DonationCheckoutLimits.DefaultCurrency;
        }

        var normalized = requestedCurrency.Trim().ToUpperInvariant();

        return DonationCheckoutLimits.SupportedCurrencies.Contains(normalized) ? normalized : null;
    }
}
