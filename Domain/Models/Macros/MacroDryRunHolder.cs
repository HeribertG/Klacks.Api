// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// One holder in the scope of a macro dry-run: the shift or absence type whose entries are evaluated, the macro it uses
/// today and the macro it would use afterwards. Both are equal for a holder that is in scope but keeps its macro.
/// </summary>
/// <param name="HolderId">Id of the shift or absence type</param>
/// <param name="CurrentMacroId">Macro the holder uses today; null when it has none</param>
/// <param name="NewMacroId">Macro the holder would use; null when an undo removes the reference</param>
public record MacroDryRunHolder(Guid HolderId, Guid? CurrentMacroId, Guid? NewMacroId);
