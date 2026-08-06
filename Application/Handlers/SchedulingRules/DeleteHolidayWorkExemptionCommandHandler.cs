// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes a holiday-work exemption. Returns false when the row is already gone, so a repeated delete
/// is not an error.
/// </summary>
/// <param name="request">Identifies the exemption to remove</param>

using Klacks.Api.Application.Commands.SchedulingRules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class DeleteHolidayWorkExemptionCommandHandler
    : BaseHandler, IRequestHandler<DeleteHolidayWorkExemptionCommand, bool>
{
    private readonly IHolidayWorkExemptionRuleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHolidayWorkExemptionCommandHandler(
        IHolidayWorkExemptionRuleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteHolidayWorkExemptionCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteHolidayWorkExemptionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var removed = await _repository.DeleteAsync(request.Id);
            if (removed == null)
            {
                return false;
            }

            await _unitOfWork.CompleteAsync();
            return true;
        },
        "deleting a holiday work exemption",
        new { request.Id });
    }
}
