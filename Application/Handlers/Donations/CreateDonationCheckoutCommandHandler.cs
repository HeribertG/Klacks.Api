// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Donations;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Donations;
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
    private static readonly string[] SupportedCurrencies = ["CHF", "EUR"];

    private const decimal MinAmount = 1m;
    private const decimal MaxAmount = 10000m;

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

            if (!SupportedCurrencies.Contains(currency))
            {
                return new CreateDonationCheckoutResponse { ErrorMessage = "Unsupported currency. Only CHF and EUR are allowed." };
            }

            if (amount < MinAmount || amount > MaxAmount)
            {
                return new CreateDonationCheckoutResponse { ErrorMessage = $"Amount must be between {MinAmount} and {MaxAmount}." };
            }

            StripeConfiguration.ApiKey = settings.SecretKey;

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SubmitType = "donate",
                SuccessUrl = settings.SuccessUrl,
                CancelUrl = settings.CancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLowerInvariant(),
                            UnitAmount = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Klacks Spende",
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
