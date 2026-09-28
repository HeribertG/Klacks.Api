// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A ready-to-execute inverse call: the skill that undoes what was done, and the complete arguments it
/// needs. Produced as DATA by the turn preparation and turned into a pending confirmation only by a real
/// chat turn, so the headless turn-eval replay can resolve an undo without writing one.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillUndoInvocation(string SkillName, IReadOnlyDictionary<string, object> Arguments);
