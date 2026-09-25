// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// One macro reference that a switch or an undo writes: the holder as it is now, the macro it uses now and the macro it
/// will use. FromMacroId is the holder's current reference as stored (it may point at a deleted macro, then From is null);
/// ToMacroId is null when an undo removes the reference.
/// </summary>
/// <param name="Holder">The shift or absence type, read before the write</param>
/// <param name="From">The macro the holder uses now; null when it has none or it no longer exists</param>
/// <param name="To">The macro the holder will use; null when an undo removes the reference</param>
public record MacroReferenceChange(MacroReferenceHolder Holder, MacroSnapshot? From, MacroSnapshot? To)
{
    public Guid? FromMacroId => Holder.MacroId;

    public Guid? ToMacroId => To?.Id;
}
