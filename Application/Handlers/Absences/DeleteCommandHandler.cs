// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting an absence with validation of existing references.
/// @param request - Contains the ID of the absence to delete
/// </summary>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Absences;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly SettingsMapper _settingsMapper;
    private readonly IAbsenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        SettingsMapper settingsMapper,
        IAbsenceRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _settingsMapper = settingsMapper;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceResource?> Handle(DeleteCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var absence = await _repository.Get(request.Id);
            if (absence == null)
            {
                throw new KeyNotFoundException($"Absence with ID {request.Id} not found.");
            }

            var usages = await GetUsagesAsync(request.Id, cancellationToken);
            if (usages.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Absence '{absence.Name}' is in use by: {string.Join(", ", usages)}. Remove these references first.");
            }

            var absenceResource = _settingsMapper.ToAbsenceResource(absence);
            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            return absenceResource;
        },
        "deleting absence",
        new { AbsenceId = request.Id });
    }

    private async Task<List<string>> GetUsagesAsync(Guid absenceId, CancellationToken cancellationToken)
    {
        var usages = new List<string>();

        var breakCount = await _repository.CountActiveBreaksByAbsenceAsync(absenceId, cancellationToken);
        if (breakCount > 0)
        {
            usages.Add($"{breakCount} Break(s)");
        }

        var placeholderCount = await _repository.CountActiveBreakPlaceholdersByAbsenceAsync(absenceId, cancellationToken);
        if (placeholderCount > 0)
        {
            usages.Add($"{placeholderCount} Break Placeholder(s)");
        }

        return usages;
    }
}
