// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// A single typed surcharge portion emitted by a macro execution.
/// </summary>
/// <param name="Type">The surcharge category (night, weekend, holiday) this portion belongs to</param>
/// <param name="Amount">The unrounded surcharge amount attributed to this category</param>
public record MacroSurchargeItem(SurchargeType Type, decimal Amount);
