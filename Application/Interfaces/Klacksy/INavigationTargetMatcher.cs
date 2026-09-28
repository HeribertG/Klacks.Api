// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Klacksy.Models;

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface INavigationTargetMatcher
{
    NavigationMatchResult Match(string normalizedUtterance, string locale, IReadOnlyCollection<string> userPermissions);
}
