// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Absences;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly SettingsMapper _settingsMapper;
    private readonly IAbsenceRepository _repository;
    private readonly DataBaseContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        SettingsMapper settingsMapper,
        IAbsenceRepository repository,
        DataBaseContext context,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _settingsMapper = settingsMapper;
        _repository = repository;
        _context = context;
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

            var usages = await GetUsagesAsync(request.Id);
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

    private async Task<List<string>> GetUsagesAsync(Guid absenceId)
    {
        var usages = new List<string>();

        var breakCount = await _context.Set<Break>()
            .CountAsync(b => !b.IsDeleted && b.AbsenceId == absenceId);
        if (breakCount > 0)
        {
            usages.Add($"{breakCount} Break(s)");
        }

        var placeholderCount = await _context.Set<BreakPlaceholder>()
            .CountAsync(bp => !bp.IsDeleted && bp.AbsenceId == absenceId);
        if (placeholderCount > 0)
        {
            usages.Add($"{placeholderCount} Break Placeholder(s)");
        }

        return usages;
    }
}
