// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partielle Klasse für Work-, Break-, Shift- und CloneShift-Mappings.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Riok.Mapperly.Abstractions;
using Staffs = Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.Api.Application.Mappers;

public partial class ScheduleMapper
{
    public partial WorkResource ToWorkResource(Work work);

    [MapperIgnoreTarget(nameof(Work.CreateTime))]
    [MapperIgnoreTarget(nameof(Work.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Work.UpdateTime))]
    [MapperIgnoreTarget(nameof(Work.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Work.DeletedTime))]
    [MapperIgnoreTarget(nameof(Work.IsDeleted))]
    [MapperIgnoreTarget(nameof(Work.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Work.AnalyseToken))]
    public partial Work ToWorkEntity(WorkResource resource);

    public partial BreakResource ToBreakResource(Break @break);

    [MapperIgnoreTarget(nameof(Break.CreateTime))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Break.UpdateTime))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Break.DeletedTime))]
    [MapperIgnoreTarget(nameof(Break.IsDeleted))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Break.AnalyseToken))]
    [MapperIgnoreTarget(nameof(Break.Client))]
    [MapperIgnoreTarget(nameof(Break.Absence))]
    public partial Break ToBreakEntity(BreakResource resource);

    public ShiftResource ToShiftResource(Shift shift)
    {
        var resource = ToShiftResourceBase(shift);
        resource.Groups = shift.GroupItems?.Select(gi => new SimpleGroupResource
        {
            Id = gi.Group?.Id ?? gi.GroupId,
            Name = gi.Group?.Name ?? string.Empty,
            Description = gi.Group?.Description ?? string.Empty,
            ValidFrom = gi.ValidFrom ?? DateTime.MinValue,
            ValidUntil = gi.ValidUntil
        }).ToList() ?? [];

        if (shift.Client != null)
        {
            resource.Client = ToShiftClientResource(shift.Client);
        }

        return resource;
    }

    private static Staffs.ClientResource ToShiftClientResource(Client client)
    {
        return new Staffs.ClientResource
        {
            Id = client.Id,
            Name = client.Name,
            FirstName = client.FirstName,
            SecondName = client.SecondName,
            MaidenName = client.MaidenName,
            Title = client.Title,
            Company = client.Company,
            Gender = client.Gender,
            IdNumber = client.IdNumber,
            LegalEntity = client.LegalEntity,
            Birthdate = client.Birthdate,
            IsDeleted = client.IsDeleted,
            Type = (int)client.Type,
            Addresses = client.Addresses?.Select(ToShiftAddressResource).ToList()
                ?? []
        };
    }

    private static Staffs.AddressResource ToShiftAddressResource(Address address)
    {
        return new Staffs.AddressResource
        {
            Id = address.Id,
            ClientId = address.ClientId,
            ValidFrom = address.ValidFrom,
            Type = address.Type,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            Street = address.Street,
            Street2 = address.Street2,
            Street3 = address.Street3,
            Zip = address.Zip,
            City = address.City,
            State = address.State,
            Country = address.Country,
            Latitude = address.Latitude,
            Longitude = address.Longitude
        };
    }

    [MapperIgnoreTarget(nameof(ShiftResource.Groups))]
    [MapperIgnoreSource(nameof(Shift.Client))]
    private partial ShiftResource ToShiftResourceBase(Shift shift);

    [MapperIgnoreTarget(nameof(Shift.CreateTime))]
    [MapperIgnoreTarget(nameof(Shift.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Shift.UpdateTime))]
    [MapperIgnoreTarget(nameof(Shift.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Shift.DeletedTime))]
    [MapperIgnoreTarget(nameof(Shift.IsDeleted))]
    [MapperIgnoreTarget(nameof(Shift.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Shift.Client))]
    [MapperIgnoreTarget(nameof(Shift.GroupItems))]
    [MapperIgnoreTarget(nameof(Shift.AnalyseToken))]
    private partial Shift ToShiftEntityBase(ShiftResource resource);

    public Shift ToShiftEntity(ShiftResource resource)
    {
        var entity = ToShiftEntityBase(resource);
        entity.GroupItems = resource.Groups?.Select(g => new GroupItem
        {
            GroupId = g.Id,
            ValidFrom = g.ValidFrom,
            ValidUntil = g.ValidUntil
        }).ToList() ?? [];
        return entity;
    }

    public partial TruncatedShiftResource ToTruncatedShiftResource(TruncatedShift truncatedShift);

    public Shift CloneShift(Shift source)
    {
        return new Shift
        {
            Id = Guid.Empty,
            CuttingAfterMidnight = source.CuttingAfterMidnight,
            Abbreviation = source.Abbreviation,
            Description = source.Description,
            MacroId = source.MacroId,
            Name = source.Name,
            Status = source.Status,
            AfterShift = source.AfterShift,
            BeforeShift = source.BeforeShift,
            EndShift = source.EndShift,
            FromDate = source.FromDate,
            StartShift = source.StartShift,
            UntilDate = source.UntilDate,
            BriefingTime = source.BriefingTime,
            DebriefingTime = source.DebriefingTime,
            TravelTimeAfter = source.TravelTimeAfter,
            TravelTimeBefore = source.TravelTimeBefore,
            IsFriday = source.IsFriday,
            IsHoliday = source.IsHoliday,
            IsMonday = source.IsMonday,
            IsSaturday = source.IsSaturday,
            IsSunday = source.IsSunday,
            IsThursday = source.IsThursday,
            IsTuesday = source.IsTuesday,
            IsWednesday = source.IsWednesday,
            IsWeekdayAndHoliday = source.IsWeekdayAndHoliday,
            IsSporadic = source.IsSporadic,
            SporadicScope = source.SporadicScope,
            IsTimeRange = source.IsTimeRange,
            Quantity = source.Quantity,
            SumEmployees = source.SumEmployees,
            WorkTime = source.WorkTime,
            ShiftType = source.ShiftType,
            OriginalId = source.OriginalId,
            ParentId = source.ParentId,
            RootId = source.RootId,
            Lft = source.Lft,
            Rgt = source.Rgt,
            ClientId = source.ClientId,
            GroupItems = source.GroupItems.Select(gi => new GroupItem
            {
                Id = Guid.Empty,
                GroupId = gi.GroupId,
                ValidFrom = gi.ValidFrom,
                ValidUntil = gi.ValidUntil,
                ShiftId = Guid.Empty
            }).ToList()
        };
    }
}
