// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpPromptCatalog
{
    IList<Prompt> ListPrompts();

    GetPromptResult GetPrompt(string name, IDictionary<string, JsonElement>? arguments);
}
