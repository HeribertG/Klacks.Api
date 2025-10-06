using FluentValidation;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.Validation.Clients;

public class ClientContractResourceValidator : AbstractValidator<ClientContractResource>
{
    public ClientContractResourceValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("address.edit-address.contracts.validation.from-date-required");

        RuleFor(x => x)
            .Must(contract =>
            {
                if (!contract.UntilDate.HasValue)
                {
                    return true;
                }
                return contract.FromDate <= contract.UntilDate.Value;
            })
            .WithMessage("address.edit-address.contracts.validation.from-date-before-until-date");
    }
}
