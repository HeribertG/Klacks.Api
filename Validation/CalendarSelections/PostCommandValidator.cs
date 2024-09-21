using FluentValidation;
using Klacks.Api.Commands;
using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Validation.CalendarSelections
{
  public class PostCommandValidator : AbstractValidator<PostCommand<CalendarSelectionResource>>
  {
    public PostCommandValidator()
    {
      RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
    }
  }
}
