// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Wires the storage of the payroll export artifacts: its own options section and root directory, separate from the
/// ERP drop-zone storage.
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Services.Exports;
using Klacks.Api.Infrastructure.Services.Exports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class PayrollExportServiceCollectionExtensions
{
    public static IServiceCollection AddPayrollArtifactStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PayrollObjectStorageOptions>(configuration.GetSection(PayrollObjectStorageOptions.SectionName));
        services.AddScoped<IPayrollArtifactStorage, PayrollArtifactStorage>();

        return services;
    }
}