// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IGroupGeocodingQueue
{
    void Queue(Guid groupId);
}
