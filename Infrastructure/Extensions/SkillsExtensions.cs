using Klacks.Api.Application.Services.Skills;

namespace Klacks.Api.Infrastructure.Extensions;

public static class SkillsExtensions
{
    public static IApplicationBuilder InitializeSkills(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var registrationService = scope.ServiceProvider.GetRequiredService<SkillRegistrationService>();
        registrationService.RegisterAllSkills();
        return app;
    }
}
