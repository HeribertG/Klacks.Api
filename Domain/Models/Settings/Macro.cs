// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings;

/// <summary>
/// A surcharge/working-time calculation script (DSL). Type carries the MacroFunctionEnum semantics
/// (Custom/Standard/StandardAdditive). Rows are either customer-created (full CRUD, ImportSourceKey
/// empty) or imported from a region-setup "macros" section (K20 entity import, ImportSourceKey/
/// ImportContentHash set — see <see cref="IImportableEntity"/>); an imported row a customer has edited
/// is never overwritten by a re-import.
/// </summary>
public class Macro : BaseEntity, IImportableEntity
{
    public string Content { get; set; } = string.Empty;

    public MultiLanguage Description { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }

    public MacroCategoryEnum Category { get; set; } = MacroCategoryEnum.Unspecified;

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
