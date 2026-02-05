using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.Api.Application.Validation.Clients;

public class PostCommandValidator : AbstractValidator<PostCommand<ClientResource>>
{
    public PostCommandValidator()
    {
        When(x => x.Resource.LegalEntity, () =>
        {
            RuleFor(x => x.Resource.Company)
                .NotEmpty()
                .WithMessage("address.edit-address.address-persona.validation.company-required");

            RuleFor(x => x.Resource.Type)
                .Equal((int)EntityTypeEnum.Customer)
                .WithMessage("address.edit-address.address-persona.validation.legal-entity-must-be-customer");

            RuleFor(x => x.Resource.Addresses)
                .Must(addresses => addresses != null && addresses.Any(a =>
                    !string.IsNullOrWhiteSpace(a.Zip) &&
                    !string.IsNullOrWhiteSpace(a.City) &&
                    !string.IsNullOrWhiteSpace(a.Country)))
                .WithMessage("address.edit-address.address-persona.validation.address-required");
        });

        When(x => !x.Resource.LegalEntity, () =>
        {
            RuleFor(x => x.Resource.FirstName)
                .NotEmpty()
                .WithMessage("address.edit-address.address-persona.validation.firstname-required");

            RuleFor(x => x.Resource.Name)
                .NotEmpty()
                .WithMessage("address.edit-address.address-persona.validation.name-required");

            RuleFor(x => x.Resource.Gender)
                .Must(gender => gender == GenderEnum.Female || gender == GenderEnum.Male || gender == GenderEnum.Intersexuality)
                .WithMessage("address.edit-address.address-persona.validation.gender-required");
        });

        RuleFor(x => x.Resource.ClientContracts)
            .Must(contracts =>
            {
                if (contracts == null || !contracts.Any())
                {
                    return true;
                }

                foreach (var contract in contracts)
                {
                    if (contract.UntilDate.HasValue && contract.FromDate > contract.UntilDate.Value)
                    {
                        return false;
                    }
                }
                return true;
            })
            .WithMessage("address.edit-address.contracts.validation.all-dates-valid");

        RuleFor(x => x.Resource.ClientContracts)
            .Must(contracts =>
            {
                if (contracts == null || !contracts.Any())
                {
                    return true;
                }
                return contracts.Any(c => c.IsActive);
            })
            .WithMessage("address.edit-address.contracts.validation.at-least-one-active");

        When(x => x.Resource.ClientImage != null, () =>
        {
            RuleFor(x => x.Resource.ClientImage!.ImageData)
                .NotEmpty()
                .WithMessage("ClientImage requires image data");

            RuleFor(x => x.Resource.ClientImage!.ContentType)
                .NotEmpty()
                .WithMessage("ClientImage requires content type");
        });
    }
}
