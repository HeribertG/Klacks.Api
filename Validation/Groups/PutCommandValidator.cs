using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Presentation.DTOs.Associations;

namespace Klacks.Api.Validation.Groups;

public class PutCommandValidator : AbstractValidator<PutCommand<GroupResource>>
{
    public PutCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
        RuleFor(x => x.Resource).Must(x => x.ValidFrom.Ticks != 0).WithMessage("ValidFrom: Valid date is required");
        RuleFor(x => x.Resource.GroupItems).Must(x =>
        {
            var list = x.Select(x => x.ClientId).Distinct().ToList();

            return x.Count == list.Count;
        }).When(x => x.Resource.GroupItems.Any()).WithMessage("The list of participants must not contain any duplicates");
    }
}
