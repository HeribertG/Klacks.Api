// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Klacksy.Models;

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface IUtteranceNormalizer
{
    NormalizedUtterance Normalize(string raw, string locale);
}
