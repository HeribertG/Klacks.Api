// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Assistant;

namespace Klacks.Api.Infrastructure.Extensions;

public static class AssistantExtensions
{
    public static IApplicationBuilder InitializeSkills(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var registrationService = scope.ServiceProvider.GetRequiredService<SkillRegistrationService>();
        registrationService.RegisterAllSkills();
        return app;
    }
}
