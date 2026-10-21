// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record CuriosityQuestion(string Topic, IReadOnlyList<string> Keywords);
