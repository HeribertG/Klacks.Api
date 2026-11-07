// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request to turn a stand-in into a real person. The same rules as creating a person apply: first name, name and a
/// person gender are required, an address and communications are optional.
/// </summary>
/// <param name="TargetType">EntityTypeEnum value after the conversion: Employee or ExternEmp</param>
/// <param name="FirstName">First name of the person</param>
/// <param name="Name">Last name of the person; replaces the stand-in's display name</param>
/// <param name="Gender">Female, Male or Intersexuality</param>
/// <param name="Address">Optional address of the person</param>
/// <param name="Communications">Optional phone numbers and e-mail addresses of the person</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Staffs;

public class ConvertStandInResource
{
    public int TargetType { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public GenderEnum Gender { get; set; }

    public AddressResource? Address { get; set; }

    public List<CommunicationResource> Communications { get; set; } = [];
}
