// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Klacksy.Models;

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface INavigationTargetMatcher
{
    NavigationMatchResult Match(string normalizedUtterance, string locale, IReadOnlyCollection<string> userPermissions);
}
