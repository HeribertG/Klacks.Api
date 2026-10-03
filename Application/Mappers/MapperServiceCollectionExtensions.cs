// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Application.Mappers;

public static class MapperServiceCollectionExtensions
{
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        services.AddSingleton<ClientMapper>();
        services.AddSingleton<ScheduleMapper>();
        services.AddSingleton<GroupMapper>();
        services.AddSingleton<SettingsMapper>();
        services.AddSingleton<AddressCommunicationMapper>();
        services.AddSingleton<AuthMapper>();
        services.AddSingleton<LLMMapper>();
        services.AddSingleton<FilterMapper>();
        services.AddSingleton<IdentityProviderMapper>();
        services.AddSingleton<ErpDropPointMapper>();
        services.AddSingleton<SkillMapper>();
        services.AddSingleton<Reports.ReportTemplateMapper>();
        services.AddSingleton<ReceivedEmailMapper>();
        services.AddSingleton<ClientAvailabilityMapper>();
        services.AddSingleton<ClientShiftPreferenceMapper>();
        services.AddSingleton<PlanningConstraintMapper>();

        return services;
    }
}
