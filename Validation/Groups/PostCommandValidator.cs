using FluentValidation;
using Klacks.Api.Commands;
using Klacks.Api.Resources.Associations;

namespace Klacks.Api.Validation.Groups;

public class PostCommandValidator : AbstractValidator<PostCommand<GroupResource>>
{
  public PostCommandValidator()
  {
    ClassLevelCascadeMode = CascadeMode.Stop;

    RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
    RuleFor(x => x.Resource).Must(x => x.ValidFrom.Ticks != 0).WithMessage("ValidFrom: Valid date is required");
    RuleFor(x => x.Resource.GroupItems).Must(x =>
    {
      var list = x.Select(x => x.ClientId).Distinct().ToList();

      return x.Count != list.Count;
    }).When(x => x.Resource.GroupItems.Any()).WithMessage("The list of participants must not contain any duplicates");
  }
}
