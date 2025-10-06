using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.Validation.Clients;

public class PutCommandValidator : AbstractValidator<PutCommand<ClientResource>>
{
    public PutCommandValidator()
    {
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
    }
}
