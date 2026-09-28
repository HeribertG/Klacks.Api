// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

/// <summary>
/// The effective answer of one provider call.
/// </summary>
/// <param name="Content">The visible answer; never taken from the reasoning channel.</param>
/// <param name="ReasoningWithoutContent">True when the model wrote reasoning but no content and no tool call - a diagnostic signal only.</param>
public readonly record struct ResolvedAnswer(string Content, bool ReasoningWithoutContent);
