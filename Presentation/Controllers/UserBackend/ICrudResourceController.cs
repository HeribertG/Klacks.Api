// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Presentation.Controllers.UserBackend;

/// <summary>
/// Marks a controller as the REST endpoint that serves TResource, so SelfApiRouteResolver can map the
/// resource to its route. InputBaseController implements it for every generic CRUD controller; a
/// controller that had to leave that base class in order to widen its authorisation declares it
/// directly, otherwise its resource would silently lose its route and every skill writing through the
/// own API would fail at the call site.
/// </summary>
/// <typeparam name="TResource">The resource DTO the controller serves</typeparam>
public interface ICrudResourceController<TResource>
{
}
