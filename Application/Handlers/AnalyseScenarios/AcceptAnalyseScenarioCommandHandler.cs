// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for accepting an AnalyseScenario.
/// Replaces the actual schedule data in the period with the scenario data.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to accept</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class AcceptAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<AcceptAnalyseScenarioCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataBaseContext _context;

    public AcceptAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        DataBaseContext context,
        ILogger<AcceptAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<bool> Handle(AcceptAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            var token = scenario.Token;
            var fromDate = scenario.FromDate;
            var untilDate = scenario.UntilDate;

            List<Guid>? shiftIds = null;
            List<Guid>? clientIds = null;

            if (scenario.GroupId.HasValue)
            {
                var groupIds = await GetGroupHierarchyIds(scenario.GroupId.Value, cancellationToken);

                shiftIds = await _context.Set<GroupItem>()
                    .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                    .Select(gi => gi.ShiftId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                clientIds = await _context.Set<GroupItem>()
                    .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
                    .Select(gi => gi.ClientId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            await SoftDeleteRealWorks(shiftIds, fromDate, untilDate, cancellationToken);
            await SoftDeleteRealBreaks(clientIds, fromDate, untilDate, cancellationToken);
            await SoftDeleteRealScheduleNotes(clientIds, fromDate, untilDate, cancellationToken);
            await PromoteScenarioData(token, cancellationToken);

            scenario.Status = AnalyseScenarioStatus.Accepted;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }

    private async Task SoftDeleteRealWorks(List<Guid>? shiftIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<Domain.Models.Schedules.Work> workQuery = _context.Work
            .Where(w => !w.IsDeleted && w.AnalyseToken == null
                && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate);

        if (shiftIds != null)
        {
            workQuery = workQuery.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var realWorks = await workQuery.ToListAsync(ct);
        var realWorkIds = realWorks.Select(w => w.Id).ToList();

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && realWorkIds.Contains(wc.WorkId))
            .ToListAsync(ct);

        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && realWorkIds.Contains(e.WorkId))
            .ToListAsync(ct);

        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = DateTime.UtcNow; }
        foreach (var exp in expenses) { exp.IsDeleted = true; exp.DeletedTime = DateTime.UtcNow; }
        foreach (var work in realWorks) { work.IsDeleted = true; work.DeletedTime = DateTime.UtcNow; }
    }

    private async Task SoftDeleteRealBreaks(List<Guid>? clientIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<Domain.Models.Schedules.Break> breakQuery = _context.Break
            .Where(b => !b.IsDeleted && b.AnalyseToken == null
                && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate);

        if (clientIds != null)
        {
            breakQuery = breakQuery.Where(b => clientIds.Contains(b.ClientId));
        }

        var realBreaks = await breakQuery.ToListAsync(ct);

        foreach (var brk in realBreaks) { brk.IsDeleted = true; brk.DeletedTime = DateTime.UtcNow; }
    }

    private async Task SoftDeleteRealScheduleNotes(List<Guid>? clientIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<Domain.Models.Schedules.ScheduleNote> noteQuery = _context.ScheduleNotes
            .Where(sn => !sn.IsDeleted && sn.AnalyseToken == null
                && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate);

        if (clientIds != null)
        {
            noteQuery = noteQuery.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var realNotes = await noteQuery.ToListAsync(ct);

        foreach (var note in realNotes) { note.IsDeleted = true; note.DeletedTime = DateTime.UtcNow; }
    }

    private async Task PromoteScenarioData(Guid token, CancellationToken ct)
    {
        var scenarioWorks = await _context.Work.IgnoreQueryFilters().Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        foreach (var w in scenarioWorks) w.AnalyseToken = null;

        var scenarioBreaks = await _context.Break.IgnoreQueryFilters().Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);
        foreach (var b in scenarioBreaks) b.AnalyseToken = null;

        var scenarioShifts = await _context.Shift.IgnoreQueryFilters().Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in scenarioShifts) s.AnalyseToken = null;

        var scenarioNotes = await _context.ScheduleNotes.IgnoreQueryFilters().Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in scenarioNotes) sn.AnalyseToken = null;
    }

    private async Task<List<Guid>> GetGroupHierarchyIds(Guid groupId, CancellationToken ct)
    {
        var allGroups = await _context.Group.Where(g => !g.IsDeleted).Select(g => new { g.Id, g.Parent }).ToListAsync(ct);
        var result = new List<Guid> { groupId };
        var queue = new Queue<Guid>();
        queue.Enqueue(groupId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var childId in allGroups.Where(g => g.Parent == currentId).Select(g => g.Id))
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }
}
