// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The container page (the weekday templates of one container shift) as a skill puts it into its result Data as
/// top-level Route. The Ui chat (chat-function-execution.service.ts, extractRouteFromResult) reads that Route from the
/// result text of any successful call and navigates there after the turn. The Angular route is registered directly under
/// /workplace as "container-template/:id" (the source folder is shift/container-template, the URL is not) and only
/// exists with the container shift id. The path itself is ProactiveActionRoutes.ContainerTemplate, so the route has one
/// literal; Klacks.Ui has no named constant for it (app-routing.module.ts and navigation.service.ts use literals).
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Constants;

public static class ContainerTemplateRoutes
{
    public const string ContainerTemplateRoute = ProactiveActionRoutes.ContainerTemplate;

    /// <summary>
    /// Route of the container page of one container shift.
    /// </summary>
    /// <param name="containerShiftId">Id of the container shift</param>
    public static string ForContainer(Guid containerShiftId) => ProactiveActionRoutes.ContainerTemplateFor(containerShiftId);
}
