using FluentValidation;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Validation.Clients;

public class FilterResourceValidator : AbstractValidator<FilterResource>
{
    public FilterResourceValidator(IClientRepository repository)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleFor(filter => filter.NumberOfItemsPerPage).NotNull().WithMessage("NumberOfItemsPerPage must be specified");
        RuleFor(filter => filter.RequiredPage).NotNull().WithMessage("RequiredPage must be specified");
        RuleFor(filter => filter.NumberOfItemsPerPage).Must(x => x > 0).WithMessage("NumberOfItemsPerPage must be greater than 0");
        RuleFor(filter => filter.RequiredPage).Must(x => x > -1).WithMessage("RequiredPage must be greater than 0");
    }
}
