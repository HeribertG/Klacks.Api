// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partial class for Work, Break, Shift and CloneShift mappings.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.DTOs.Filter;
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
    public partial Work ToWorkEntity(WorkResource resource);

    public partial BreakResource ToBreakResource(Break @break);

    [MapperIgnoreTarget(nameof(Break.CreateTime))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Break.UpdateTime))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Break.DeletedTime))]
    [MapperIgnoreTarget(nameof(Break.IsDeleted))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserDeleted))]
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

        resource.DefaultExpenses = shift.ShiftExpenses?.Select(e => new ShiftExpensesResource
        {
            Id = e.Id,
            ShiftId = e.ShiftId,
            Amount = e.Amount,
            Description = e.Description,
            Taxable = e.Taxable
        }).ToList() ?? [];

        resource.RequiredQualifications = shift.RequiredQualifications?.Select(q => new ShiftRequiredQualificationResource
        {
            Id = q.Id,
            ShiftId = q.ShiftId,
            QualificationId = q.QualificationId,
            IsMandatory = q.IsMandatory,
            MinLevel = q.MinLevel
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
    [MapperIgnoreTarget(nameof(ShiftResource.DefaultExpenses))]
    [MapperIgnoreTarget(nameof(ShiftResource.RequiredQualifications))]
    [MapperIgnoreSource(nameof(Shift.Client))]
    [MapperIgnoreSource(nameof(Shift.ShiftExpenses))]
    [MapperIgnoreSource(nameof(Shift.RequiredQualifications))]
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
    [MapperIgnoreTarget(nameof(Shift.ShiftExpenses))]
    [MapperIgnoreTarget(nameof(Shift.RequiredQualifications))]
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
        entity.ShiftExpenses = resource.DefaultExpenses?.Select(e => new ShiftExpenses
        {
            Id = e.Id,
            ShiftId = e.ShiftId,
            Amount = e.Amount,
            Description = e.Description,
            Taxable = e.Taxable
        }).ToList() ?? [];
        entity.RequiredQualifications = resource.RequiredQualifications?
            .Where(q => q.QualificationId != Guid.Empty)
            .Select(q => new ShiftRequiredQualification
            {
                Id = q.Id,
                ShiftId = q.ShiftId,
                QualificationId = q.QualificationId,
                IsMandatory = q.IsMandatory,
                MinLevel = q.MinLevel
            }).ToList() ?? [];
        return entity;
    }

    public partial TruncatedShiftResource ToTruncatedShiftResource(TruncatedShift truncatedShift);

    public Shift CloneShift(Shift source) => ShiftCloner.Clone(source);
}
