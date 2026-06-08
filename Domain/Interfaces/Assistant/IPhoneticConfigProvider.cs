// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface IPhoneticConfigProvider
{
    PhoneticConfig GetForLocale(string? locale);
}
