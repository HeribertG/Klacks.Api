// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IOrderImportParser
{
    OrderImportParseResult Parse(Stream xmlStream);
}
