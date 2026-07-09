// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reference to the entity a name slot is expected to resolve to, identified by the
/// visible id number instead of a database GUID so goldsets stay portable across databases.
/// </summary>
/// <param name="Type">Entity kind, e.g. "client"</param>
/// <param name="IdNumber">Visible id number of the expected entity</param>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public sealed record ExpectedEntityRef(string Type, int IdNumber);
