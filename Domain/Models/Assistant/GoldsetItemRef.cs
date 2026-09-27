// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One goldset item, named by its goldset file and its item id.
/// </summary>
/// <param name="Goldset">Goldset name as the loader takes it, e.g. turn-selection-v1</param>
/// <param name="ItemId">Item id inside that goldset</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoldsetItemRef(string Goldset, string ItemId);
