using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Validation.BreakPlaceholders;

public class PostCommandValidator : AbstractValidator<PostCommand<BreakPlaceholderResource>>
{
    private readonly IClientRepository _clientRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly ILogger<PostCommandValidator> _logger;

    public PostCommandValidator(
        IClientRepository clientRepository,
        IAbsenceRepository absenceRepository,
        ILogger<PostCommandValidator> logger)
    {
        _clientRepository = clientRepository;
        _absenceRepository = absenceRepository;
        this._logger = logger;

        RuleFor(x => x.Resource.ClientId)
            .NotEmpty().WithMessage("ClientId is required");

        RuleFor(x => x.Resource.AbsenceId)
            .NotEmpty().WithMessage("AbsenceId is required");

        RuleFor(x => x.Resource.From)
            .NotEmpty().WithMessage("From date is required");

        RuleFor(x => x.Resource.Until)
            .NotEmpty().WithMessage("Until date is required")
            .GreaterThanOrEqualTo(x => x.Resource.From).WithMessage("Until date must be on or after From date");

        RuleFor(x => x.Resource)
            .MustAsync(async (breakResource, cancellation) =>
            {
                try
                {
                    var client = await _clientRepository.GetWithMembershipAsync(breakResource.ClientId, cancellation);

                    if (client == null)
                    {
                        return false;
                    }

                    if (client.Membership == null)
                    {
                        _logger.LogWarning("Employee {ClientId} has no membership", breakResource.ClientId);
                        return false;
                    }

                    if (breakResource.From < client.Membership.ValidFrom)
                    {
                        return false;
                    }

                    if (client.Membership.ValidUntil.HasValue &&
                        breakResource.Until > client.Membership.ValidUntil.Value)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating membership period for client {ClientId}", breakResource.ClientId);
                    return false;
                }
            })
            .WithMessage("Break must be within the client's membership validity period (ValidFrom to ValidUntil)");

        RuleFor(x => x.Resource.AbsenceId)
            .MustAsync(async (absenceId, cancellation) =>
            {
                try
                {
                    return await _absenceRepository.Exists(absenceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating absence {AbsenceId}", absenceId);
                    return false;
                }
            })
            .WithMessage("Invalid AbsenceId - absence type does not exist");

    }
}
