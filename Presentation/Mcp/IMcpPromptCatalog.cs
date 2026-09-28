// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpPromptCatalog
{
    Task<IList<Prompt>> ListPromptsAsync(CancellationToken cancellationToken = default);

    Task<GetPromptResult> GetPromptAsync(string name, IDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken = default);
}
