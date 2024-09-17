using FluentValidation;
using Klacks_api.Commands;
using Klacks_api.Resources.Schedules;

namespace Klacks_api.Validation.CalendarSelections
{
  public class PutCommandValidator : AbstractValidator<PutCommand<CalendarSelectionResource>>
  {
    public PutCommandValidator()
    {
      RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
    }
  }
}
