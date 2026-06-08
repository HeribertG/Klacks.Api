// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPhoneticEncoder
{
    string Encode(string word);
}
