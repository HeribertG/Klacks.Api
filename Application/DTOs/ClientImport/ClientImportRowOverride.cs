// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportRowOverride
{
    public int RowIndex { get; set; }

    public bool? Skip { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<GenderEnum>))]
    public GenderEnum? Gender { get; set; }

    public bool? CreateDuplicate { get; set; }
}
