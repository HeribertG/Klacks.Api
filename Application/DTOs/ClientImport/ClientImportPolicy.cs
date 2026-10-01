// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportPolicy
{
    public Guid? ContractId { get; set; }

    public Guid? GroupId { get; set; }

    public string? EntryDate { get; set; }

    public string? DefaultCountry { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<CommunicationTypeEnum>))]
    public CommunicationTypeEnum EmailType { get; set; } = CommunicationTypeEnum.PrivateMail;

    [JsonConverter(typeof(JsonStringEnumConverter<CommunicationTypeEnum>))]
    public CommunicationTypeEnum PhoneType { get; set; } = CommunicationTypeEnum.PrivateFixPhone;

    [JsonConverter(typeof(JsonStringEnumConverter<CommunicationTypeEnum>))]
    public CommunicationTypeEnum MobileType { get; set; } = CommunicationTypeEnum.PrivateCellPhone;

    public ClientImportFormerEmployeesMode FormerEmployees { get; set; } = ClientImportFormerEmployeesMode.Skip;

    public ClientImportDuplicateMode Duplicates { get; set; } = ClientImportDuplicateMode.Skip;
}
