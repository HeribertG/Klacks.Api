// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public class WorkScheduleClientResource
{
    public Guid Id { get; set; }
    public string? Company { get; set; }
    public string? FirstName { get; set; }
    public string? Name { get; set; }
    public string? SecondName { get; set; }
    public string? Title { get; set; }
    public string? MaidenName { get; set; }
    public GenderEnum Gender { get; set; }
    public int IdNumber { get; set; }
    public bool LegalEntity { get; set; }
    public int Type { get; set; }
    public bool HasContract { get; set; }
}
