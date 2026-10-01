// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ClientImportIssueSeverity>))]
public enum ClientImportIssueSeverity
{
    Error,
    Warning,
    Info,
}
