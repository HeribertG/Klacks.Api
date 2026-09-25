// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroOutputChannelInspector
{
    MacroOutputChannelScan Inspect(string content);
}
