// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IOrderImportParser
{
    OrderImportParseResult Parse(Stream xmlStream);
}
