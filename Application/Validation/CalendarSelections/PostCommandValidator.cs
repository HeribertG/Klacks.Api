using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Validation.CalendarSelections
{
    public class PostCommandValidator : AbstractValidator<PostCommand<CalendarSelectionResource>>
    {
        public PostCommandValidator()
        {
            RuleFor(x => x.Resource).Must(x => !string.IsNullOrEmpty(x.Name)).WithMessage("Name is required");
        }
    }
}
