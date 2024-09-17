using FluentValidation;
using Klacks_api.Commands;
using Klacks_api.Resources.Schedules;

namespace Klacks_api.Validation.CalendarSelections
{
  public class PostCommandValidator : AbstractValidator<PostCommand<CalendarSelectionResource>>
  {
    public PostCommandValidator()
    {
      RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
    }
  }
}
