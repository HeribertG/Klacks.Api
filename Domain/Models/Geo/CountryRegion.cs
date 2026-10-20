// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Geo;

/// <summary>
/// One region of a country's region file: a stable key and its display name per language.
/// </summary>
/// <param name="Key">Language-independent identity of the region, unique within the country (e.g. "west").</param>
/// <param name="Names">Display name per language; always holds every core language.</param>
public sealed record CountryRegion(string Key, MultiLanguage Names);
