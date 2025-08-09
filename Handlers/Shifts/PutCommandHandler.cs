using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Datas;
using Klacks.Api.Enums;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Migrations;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class PutCommandHandler : IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IShiftRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                            IMapper mapper,
                            IShiftRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.BeginTransactionAsync();

        try
        {
            var dbShift = await repository.Get(request.Resource.Id);
            if (dbShift == null)
            {
                logger.LogWarning("Shift with ID {ShiftId} not found.", request.Resource.Id);
                return null;
            }

            var idList = request.Resource.Groups.Select(x => x.Id).ToList();

            request.Resource.Groups.Clear();

            var oldStatus = dbShift.Status;
            var newStatus = request.Resource.Status;

            var updatedShift = mapper.Map(request.Resource, dbShift);

            await StoreInRepostitory(updatedShift, idList, oldStatus, newStatus);

            await unitOfWork.CommitTransactionAsync(transaction);

            logger.LogInformation("Shift with ID {ShiftId} updated successfully.", request.Resource.Id);

            var getShift = await repository.Get(request.Resource.Id);

            return mapper.Map<Shift, ShiftResource>(getShift!);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(transaction);
            logger.LogError(ex, "Error occurred while updating shift with ID {ShiftId}.", request.Resource.Id);
            throw new InvalidRequestException("Error occurred while updating shift with ID {ShiftId}. " + ex.Message);
        }
    }

    private async Task StoreInRepostitory(Shift shift, List<Guid> groupIdList, ShiftStatus oldStatus, ShiftStatus newStatus)
    { 
        if (oldStatus == ShiftStatus.Original && newStatus == ShiftStatus.ReadyToCut)
        {
                 var originalId = shift.Id;
            
            var newShift = CreateShiftCopy(shift, originalId, ShiftStatus.IsCutOriginal);
                        
            shift.Status = ShiftStatus.ReadyToCut;
            await Store(shift, groupIdList);
            await unitOfWork.CompleteAsync();
            
            await repository.Add(newShift);
            await repository.UpdateGroupItems(newShift.Id, groupIdList);
            await unitOfWork.CompleteAsync();
        }
        else
        {
            await Store(shift, groupIdList);
        }
    }

    private Shift CreateShiftCopy(Shift originalShift, Guid originalId,  ShiftStatus status)
    {
        var newGuid = Guid.NewGuid();

        return new Shift
        {
            Id = newGuid,
            Name = originalShift.Name,
            Description = originalShift.Description,
            Abbreviation = originalShift.Abbreviation,
            FromDate = originalShift.FromDate,
            UntilDate = originalShift.UntilDate,
            StartShift = originalShift.StartShift,
            EndShift = originalShift.EndShift,
            BeforeShift = originalShift.BeforeShift,
            AfterShift = originalShift.AfterShift,
            WorkTime = originalShift.WorkTime,
            Quantity = originalShift.Quantity,
            SumEmployees = originalShift.SumEmployees,
            IsMonday = originalShift.IsMonday,
            IsTuesday = originalShift.IsTuesday,
            IsWednesday = originalShift.IsWednesday,
            IsThursday = originalShift.IsThursday,
            IsFriday = originalShift.IsFriday,
            IsSaturday = originalShift.IsSaturday,
            IsSunday = originalShift.IsSunday,
            IsHoliday = originalShift.IsHoliday,
            IsWeekdayOrHoliday = originalShift.IsWeekdayOrHoliday,
            ClientId = originalShift.ClientId,
            MacroId = originalShift.MacroId,
            IsSporadic = originalShift.IsSporadic,
            SporadicScope = originalShift.SporadicScope,
            IsTimeRange = originalShift.IsTimeRange,
            ShiftType = originalShift.ShiftType,
            BriefingTime = originalShift.BriefingTime,
            DebriefingTime = originalShift.DebriefingTime,
            TravelTimeAfter = originalShift.TravelTimeAfter,
            TravelTimeBefore = originalShift.TravelTimeBefore,
            CuttingAfterMidnight = originalShift.CuttingAfterMidnight,
            OriginalId = originalId,
            RootId = newGuid,
            Lft=1,
            Rgt=2,
            Status = status,  
        };
    }

    private async Task Store(Shift shift, List<Guid> groupIdList)
    {
        await repository.Put(shift);

        await repository.UpdateGroupItems(shift.Id, groupIdList);
    }

    private async Task StorePost(Shift shift, List<Guid> groupIdList)
    {
        await repository.Add(shift);

        await repository.UpdateGroupItems(shift.Id, groupIdList);
    }
}
