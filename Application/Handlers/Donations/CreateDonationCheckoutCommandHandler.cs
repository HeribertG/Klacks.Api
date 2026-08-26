// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Donations;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Donations;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Klacks.Api.Application.Handlers.Donations;

/// <summary>
/// Creates a Stripe Checkout session for a donation. The Stripe secret key is
/// read from server configuration only and never leaves the backend.
/// </summary>
public class CreateDonationCheckoutCommandHandler : BaseHandler, IRequestHandler<CreateDonationCheckoutCommand, CreateDonationCheckoutResponse>
{
    private const string DonationProductName = "Klacks Spende";
    private const string PaymentMode = "payment";
    private const string DonateSubmitType = "donate";
    private const int SingleLineItemQuantity = 1;
    private const int MinorUnitFactor = 100;

    private readonly IOptions<StripeSettings> stripeOptions;

    public CreateDonationCheckoutCommandHandler(
        IOptions<StripeSettings> stripeOptions,
        ILogger<CreateDonationCheckoutCommandHandler> logger)
        : base(logger)
    {
        this.stripeOptions = stripeOptions;
    }

    public async Task<CreateDonationCheckoutResponse> Handle(CreateDonationCheckoutCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var settings = this.stripeOptions.Value;
            var currency = command.Request.Currency?.Trim().ToUpperInvariant() ?? string.Empty;
            var amount = command.Request.Amount;

            if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                return new CreateDonationCheckoutResponse { ErrorMessage = "Donation checkout is not configured." };
            }

            if (string.IsNullOrWhiteSpace(settings.SuccessUrl) || string.IsNullOrWhiteSpace(settings.CancelUrl))
            {
                return new CreateDonationCheckoutResponse { ErrorMessage = "Donation checkout return URLs are not configured." };
            }

            if (!DonationCheckoutLimits.SupportedCurrencies.Contains(currency))
            {
                return new CreateDonationCheckoutResponse
                {
                    ErrorMessage = "Unsupported currency. Only " +
                        $"{string.Join(" and ", DonationCheckoutLimits.SupportedCurrencies)} are allowed."
                };
            }

            if (amount < DonationCheckoutLimits.MinAmount || amount > DonationCheckoutLimits.MaxAmount)
            {
                return new CreateDonationCheckoutResponse
                {
                    ErrorMessage = $"Amount must be between {DonationCheckoutLimits.MinAmount} and " +
                        $"{DonationCheckoutLimits.MaxAmount}."
                };
            }

            StripeConfiguration.ApiKey = settings.SecretKey;

            var options = new SessionCreateOptions
            {
                Mode = PaymentMode,
                SubmitType = DonateSubmitType,
                SuccessUrl = settings.SuccessUrl,
                CancelUrl = settings.CancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = SingleLineItemQuantity,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLowerInvariant(),
                            UnitAmount = (long)Math.Round(amount * MinorUnitFactor, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = DonationProductName,
                            },
                        },
                    },
                ],
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return new CreateDonationCheckoutResponse { Url = session.Url };
        }, nameof(Handle));
    }
}
