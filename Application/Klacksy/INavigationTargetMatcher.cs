// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Klacksy.Models;

public interface INavigationTargetMatcher
{
    NavigationMatchResult Match(string normalizedUtterance, string locale, IReadOnlyCollection<string> userPermissions);
}
