using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Validation.Breaks;

public class PostCommandValidator : AbstractValidator<PostCommand<BreakResource>>
{
    private readonly DataBaseContext _context;
    private readonly ILogger<PostCommandValidator> _logger;

    public PostCommandValidator(DataBaseContext context, ILogger<PostCommandValidator> logger)
    {
        _context = context;
        _logger = logger;

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
                    var client = await _context.Client
                        .Include(c => c.Membership)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == breakResource.ClientId, cancellation);

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
                    return await _context.Absence.AnyAsync(a => a.Id == absenceId, cancellation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating absence {AbsenceId}", absenceId);
                    return false;
                }
            })
            .WithMessage("Invalid AbsenceId - absence type does not exist");

        RuleFor(x => x.Resource)
            .MustAsync(async (breakResource, cancellation) => 
            {
                try
                {
                    var hasOverlap = await _context.Break
                        .AsNoTracking()
                        .Where(b => b.ClientId == breakResource.ClientId)
                        .Where(b => 
                            (b.From < breakResource.Until && b.Until > breakResource.From))
                        .AnyAsync(cancellation);

                    return !hasOverlap;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking for overlapping breaks for client {ClientId}", breakResource.ClientId);
                    return false;
                }
            })
            .WithMessage("Break overlaps with an existing break for this client");
    }
}