// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ClientImportDuplicateMode>))]
public enum ClientImportDuplicateMode
{
    Skip,
    CreateAnyway,
}
