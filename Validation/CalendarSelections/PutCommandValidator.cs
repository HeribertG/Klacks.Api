using FluentValidation;
using Klacks.Api.Commands;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Validation.CalendarSelections
{
    public class PutCommandValidator : AbstractValidator<PutCommand<CalendarSelectionResource>>
    {
        public PutCommandValidator()
        {
            RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
        }
    }
}
