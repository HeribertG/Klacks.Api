// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Identifies the macro switch to undo by its switch id, as reported when the macro was switched. The whole switch is
/// undone. A holder id is deliberately not accepted: it would resolve to the holder's latest switch at the moment of the
/// call, which can be a different, younger switch than the one the user confirmed.
/// </summary>
/// <param name="SwitchId">Id of the recorded switch, as reported when the macro was switched</param>
public record MacroRevertRequest(Guid SwitchId);
