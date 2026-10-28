// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Ambient Playground visitor of the request being processed. Deliberately an AsyncLocal and not a scoped
/// service: LLM calls run in child DI scopes and detached tasks, which receive the value through the
/// captured execution context. It must be set in the request flow itself (a middleware before the
/// pipeline), never inside an async iterator, because an assignment made between two yields does not
/// survive the yield boundary. Work outside a request (hosted services, MCP without an address) sees null.
/// </summary>

using Klacks.Api.Domain.Models.Playground;

namespace Klacks.Api.Domain.Services.Playground;

public static class PlaygroundVisitorContext
{
    private static readonly AsyncLocal<PlaygroundVisitor?> CurrentVisitor = new();

    public static PlaygroundVisitor? Current => CurrentVisitor.Value;

    /// <param name="visitor">The visitor to publish for the rest of the current async flow</param>
    public static void Set(PlaygroundVisitor visitor) => CurrentVisitor.Value = visitor;

    public static void Clear() => CurrentVisitor.Value = null;
}
