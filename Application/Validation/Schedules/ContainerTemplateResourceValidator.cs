using FluentValidation;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Validation.Schedules;

public class ContainerTemplateResourceValidator : AbstractValidator<ContainerTemplateResource>
{
    private readonly IShiftRepository _shiftRepository;

    public ContainerTemplateResourceValidator(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;

        RuleFor(x => x.ContainerId)
            .NotEmpty().WithMessage("Container Shift ID is required")
            .MustAsync(BeValidContainerShift).WithMessage("Container Shift must be an OriginalShift with ShiftType IsContainer");

        RuleFor(x => x.FromTime)
            .NotEmpty().WithMessage("From Time is required");

        RuleFor(x => x.UntilTime)
            .NotEmpty().WithMessage("Until Time is required");

        RuleFor(x => x.Weekday)
            .InclusiveBetween(0, 8).WithMessage("Weekday must be between 0 and 8");
    }

    private async Task<bool> BeValidContainerShift(Guid containerId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.Get(containerId);

        if (shift == null)
            return false;

        return shift.Status == ShiftStatus.OriginalShift && shift.ShiftType == ShiftType.IsContainer;
    }
}
