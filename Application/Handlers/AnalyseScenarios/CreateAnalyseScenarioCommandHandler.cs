// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a new AnalyseScenario by cloning all schedule data.
/// </summary>
/// <param name="Request">Contains name, description, optional group and time period</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class CreateAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<CreateAnalyseScenarioCommand, AnalyseScenarioResource>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataBaseContext _context;

    public CreateAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        DataBaseContext context,
        ILogger<CreateAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<AnalyseScenarioResource> Handle(CreateAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var token = Guid.NewGuid();
            var fromDate = command.Request.FromDate;
            var untilDate = command.Request.UntilDate;
            var groupId = command.Request.GroupId;

            var scenario = new AnalyseScenario
            {
                Name = command.Request.Name,
                Description = command.Request.Description,
                GroupId = groupId,
                FromDate = fromDate,
                UntilDate = untilDate,
                Token = token
            };

            await _repository.Add(scenario);

            var groupIds = groupId.HasValue
                ? await GetGroupHierarchyIds(groupId.Value, cancellationToken)
                : null;

            var shiftIdMap = await CloneShifts(groupIds, token, cancellationToken);
            await CloneWorks(groupIds, fromDate, untilDate, token, shiftIdMap, cancellationToken);
            await CloneBreaks(groupIds, fromDate, untilDate, token, cancellationToken);
            await CloneScheduleNotes(groupIds, fromDate, untilDate, token, cancellationToken);

            await _unitOfWork.CompleteAsync();

            return new AnalyseScenarioResource
            {
                Id = scenario.Id,
                Name = scenario.Name,
                Description = scenario.Description,
                GroupId = scenario.GroupId,
                FromDate = scenario.FromDate,
                UntilDate = scenario.UntilDate,
                Token = scenario.Token,
                CreatedByUser = scenario.CreatedByUser,
                Status = (int)scenario.Status
            };
        }, nameof(Handle), new { command.Request.Name, command.Request.GroupId });
    }

    private async Task<List<Guid>> GetGroupHierarchyIds(Guid groupId, CancellationToken ct)
    {
        var allGroups = await _context.Group
            .Where(g => !g.IsDeleted)
            .Select(g => new { g.Id, g.Parent })
            .ToListAsync(ct);

        var result = new List<Guid> { groupId };
        var queue = new Queue<Guid>();
        queue.Enqueue(groupId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = allGroups.Where(g => g.Parent == currentId).Select(g => g.Id);
            foreach (var childId in children)
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    private async Task<Dictionary<Guid, Guid>> CloneShifts(List<Guid>? groupIds, Guid token, CancellationToken ct)
    {
        IQueryable<Shift> shiftQuery = _context.Shift
            .Where(s => !s.IsDeleted && s.AnalyseToken == null);

        if (groupIds != null)
        {
            var shiftIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                .Select(gi => gi.ShiftId!.Value)
                .Distinct()
                .ToListAsync(ct);

            shiftQuery = shiftQuery.Where(s => shiftIds.Contains(s.Id));
        }

        var shifts = await shiftQuery
            .Include(s => s.GroupItems.Where(gi => !gi.IsDeleted))
            .AsNoTracking()
            .ToListAsync(ct);

        var idMap = new Dictionary<Guid, Guid>();

        foreach (var shift in shifts)
        {
            var newId = Guid.NewGuid();
            idMap[shift.Id] = newId;
        }

        foreach (var shift in shifts)
        {
            var clone = new Shift
            {
                Id = idMap[shift.Id],
                AnalyseToken = token,
                CuttingAfterMidnight = shift.CuttingAfterMidnight,
                Abbreviation = shift.Abbreviation,
                Description = shift.Description,
                MacroId = shift.MacroId,
                Name = shift.Name,
                Status = shift.Status,
                AfterShift = shift.AfterShift,
                BeforeShift = shift.BeforeShift,
                EndShift = shift.EndShift,
                FromDate = shift.FromDate,
                StartShift = shift.StartShift,
                UntilDate = shift.UntilDate,
                BriefingTime = shift.BriefingTime,
                DebriefingTime = shift.DebriefingTime,
                TravelTimeAfter = shift.TravelTimeAfter,
                TravelTimeBefore = shift.TravelTimeBefore,
                IsFriday = shift.IsFriday,
                IsHoliday = shift.IsHoliday,
                IsMonday = shift.IsMonday,
                IsSaturday = shift.IsSaturday,
                IsSunday = shift.IsSunday,
                IsThursday = shift.IsThursday,
                IsTuesday = shift.IsTuesday,
                IsWednesday = shift.IsWednesday,
                IsWeekdayAndHoliday = shift.IsWeekdayAndHoliday,
                IsSporadic = shift.IsSporadic,
                SporadicScope = shift.SporadicScope,
                IsTimeRange = shift.IsTimeRange,
                Quantity = shift.Quantity,
                SumEmployees = shift.SumEmployees,
                WorkTime = shift.WorkTime,
                ShiftType = shift.ShiftType,
                OriginalId = shift.OriginalId.HasValue && idMap.ContainsKey(shift.OriginalId.Value) ? idMap[shift.OriginalId.Value] : null,
                ParentId = shift.ParentId.HasValue && idMap.ContainsKey(shift.ParentId.Value) ? idMap[shift.ParentId.Value] : null,
                RootId = shift.RootId.HasValue && idMap.ContainsKey(shift.RootId.Value) ? idMap[shift.RootId.Value] : null,
                Lft = shift.Lft,
                Rgt = shift.Rgt,
                ClientId = shift.ClientId,
                GroupItems = shift.GroupItems.Select(gi => new GroupItem
                {
                    Id = Guid.NewGuid(),
                    GroupId = gi.GroupId,
                    ClientId = gi.ClientId,
                    ShiftId = idMap[shift.Id]
                }).ToList()
            };

            await _context.Shift.AddAsync(clone, ct);
        }

        return idMap;
    }

    private async Task CloneWorks(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, Dictionary<Guid, Guid> shiftIdMap, CancellationToken ct)
    {
        IQueryable<Work> workQuery = _context.Work
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate);

        if (groupIds != null)
        {
            var shiftIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                .Select(gi => gi.ShiftId!.Value)
                .Distinct()
                .ToListAsync(ct);

            workQuery = workQuery.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var works = await workQuery.AsNoTracking().ToListAsync(ct);

        var workIdMap = new Dictionary<Guid, Guid>();

        foreach (var work in works)
        {
            var newWorkId = Guid.NewGuid();
            workIdMap[work.Id] = newWorkId;

            var clone = new Work
            {
                Id = newWorkId,
                AnalyseToken = token,
                ClientId = work.ClientId,
                CurrentDate = work.CurrentDate,
                Information = work.Information,
                WorkTime = work.WorkTime,
                Surcharges = work.Surcharges,
                StartTime = work.StartTime,
                EndTime = work.EndTime,
                LockLevel = work.LockLevel,
                SealedAt = work.SealedAt,
                SealedBy = work.SealedBy,
                ShiftId = shiftIdMap.TryGetValue(work.ShiftId, out var newShiftId) ? newShiftId : work.ShiftId
            };

            await _context.Work.AddAsync(clone, ct);
        }

        await CloneWorkChanges(workIdMap, ct);
        await CloneExpenses(workIdMap, ct);
    }

    private async Task CloneWorkChanges(Dictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && originalWorkIds.Contains(wc.WorkId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var wc in workChanges)
        {
            var clone = new WorkChange
            {
                Id = Guid.NewGuid(),
                WorkId = workIdMap[wc.WorkId],
                StartTime = wc.StartTime,
                EndTime = wc.EndTime,
                ChangeTime = wc.ChangeTime,
                Surcharges = wc.Surcharges,
                Type = wc.Type,
                Description = wc.Description,
                ToInvoice = wc.ToInvoice,
                ReplaceClientId = wc.ReplaceClientId
            };

            await _context.WorkChange.AddAsync(clone, ct);
        }
    }

    private async Task CloneExpenses(Dictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && originalWorkIds.Contains(e.WorkId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var exp in expenses)
        {
            var clone = new Domain.Models.Schedules.Expenses
            {
                Id = Guid.NewGuid(),
                WorkId = workIdMap[exp.WorkId],
                Description = exp.Description,
                Amount = exp.Amount,
                Taxable = exp.Taxable
            };

            await _context.Expenses.AddAsync(clone, ct);
        }
    }

    private async Task CloneBreaks(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, CancellationToken ct)
    {
        IQueryable<Break> breakQuery = _context.Break
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate);

        if (groupIds != null)
        {
            var clientIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
                .Select(gi => gi.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);

            breakQuery = breakQuery.Where(b => clientIds.Contains(b.ClientId));
        }

        var breaks = await breakQuery.AsNoTracking().ToListAsync(ct);

        foreach (var brk in breaks)
        {
            var clone = new Break
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = brk.ClientId,
                CurrentDate = brk.CurrentDate,
                Information = brk.Information,
                WorkTime = brk.WorkTime,
                Surcharges = brk.Surcharges,
                StartTime = brk.StartTime,
                EndTime = brk.EndTime,
                LockLevel = brk.LockLevel,
                SealedAt = brk.SealedAt,
                SealedBy = brk.SealedBy,
                AbsenceId = brk.AbsenceId,
                Description = brk.Description
            };

            await _context.Break.AddAsync(clone, ct);
        }
    }

    private async Task CloneScheduleNotes(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, CancellationToken ct)
    {
        IQueryable<ScheduleNote> noteQuery = _context.ScheduleNotes
            .Where(sn => !sn.IsDeleted
                && sn.AnalyseToken == null
                && sn.CurrentDate >= fromDate
                && sn.CurrentDate <= untilDate);

        if (groupIds != null)
        {
            var clientIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
                .Select(gi => gi.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);

            noteQuery = noteQuery.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var notes = await noteQuery.AsNoTracking().ToListAsync(ct);

        foreach (var note in notes)
        {
            var clone = new ScheduleNote
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = note.ClientId,
                CurrentDate = note.CurrentDate,
                Content = note.Content
            };

            await _context.ScheduleNotes.AddAsync(clone, ct);
        }
    }

}
