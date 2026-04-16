// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting an AnalyseScenario. Also soft-deletes every
/// remaining clone that still carries the scenario token, so no orphaned
/// sub-works or sub-breaks leak back into the real schedule.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to delete</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class DeleteAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<DeleteAnalyseScenarioCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataBaseContext _context;

    public DeleteAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        DataBaseContext context,
        ILogger<DeleteAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<bool> Handle(DeleteAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId);

            if (scenario == null)
            {
                throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");
            }

            await SoftDeleteScenarioData(scenario.Token, cancellationToken);
            await _repository.Delete(command.ScenarioId);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }

    private async Task SoftDeleteScenarioData(Guid token, CancellationToken ct)
    {
        var works = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        var workIds = works.Select(w => w.Id).ToList();

        var orphanSubWorks = workIds.Count == 0
            ? new List<Domain.Models.Schedules.Work>()
            : await _context.Work.IgnoreQueryFilters()
                .Where(w => !w.IsDeleted
                    && w.ParentWorkId.HasValue
                    && workIds.Contains(w.ParentWorkId.Value))
                .ToListAsync(ct);
        var allWorkIds = workIds.Concat(orphanSubWorks.Select(w => w.Id)).ToList();

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && allWorkIds.Contains(wc.WorkId)).ToListAsync(ct);
        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = DateTime.UtcNow; }

        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && allWorkIds.Contains(e.WorkId)).ToListAsync(ct);
        foreach (var exp in expenses) { exp.IsDeleted = true; exp.DeletedTime = DateTime.UtcNow; }

        foreach (var w in works) { w.IsDeleted = true; w.DeletedTime = DateTime.UtcNow; }
        foreach (var w in orphanSubWorks) { w.IsDeleted = true; w.DeletedTime = DateTime.UtcNow; }

        var breaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);
        foreach (var b in breaks) { b.IsDeleted = true; b.DeletedTime = DateTime.UtcNow; }

        var orphanSubBreaks = workIds.Count == 0
            ? new List<Domain.Models.Schedules.Break>()
            : await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted
                    && b.ParentWorkId.HasValue
                    && workIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
        foreach (var b in orphanSubBreaks) { b.IsDeleted = true; b.DeletedTime = DateTime.UtcNow; }

        var shifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in shifts) { s.IsDeleted = true; s.DeletedTime = DateTime.UtcNow; }

        var notes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in notes) { sn.IsDeleted = true; sn.DeletedTime = DateTime.UtcNow; }
    }
}
