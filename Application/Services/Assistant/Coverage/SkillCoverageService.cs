// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses docs/klacksy-usecases.md and counts the status cells per row to compute
/// the autonomy-roadmap S10 coverage metric. The use-case file is the single source
/// of truth; skills are not consulted directly so the metric is decoupled from the
/// agent_skills table (skills can exist without being mapped to a documented use case).
/// </summary>
/// <param name="hostEnvironment">Used to resolve the docs path relative to ContentRootPath.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Hosting;

namespace Klacks.Api.Application.Services.Assistant.Coverage;

public class SkillCoverageService : ISkillCoverageService
{
    private const string UseCaseFileRelative = "../docs/klacksy-usecases.md";
    private const string CoveredMarker = "✅";
    private const string PartialMarker = "🟡";
    private const string MissingMarker = "❌";

    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<SkillCoverageService> _logger;

    public SkillCoverageService(IHostEnvironment hostEnvironment, ILogger<SkillCoverageService> logger)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<SkillCoverageReport> ComputeAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_hostEnvironment.ContentRootPath, UseCaseFileRelative);
        if (!File.Exists(path))
        {
            _logger.LogWarning("klacksy-usecases.md not found at {Path}; returning empty report", path);
            return new SkillCoverageReport(0, 0, 0, 0, 0d, DateTime.UtcNow);
        }

        var covered = 0;
        var partial = 0;
        var missing = 0;

        await foreach (var line in File.ReadLinesAsync(path, cancellationToken))
        {
            if (!IsTableRow(line)) continue;
            if (line.Contains(CoveredMarker, StringComparison.Ordinal)) covered++;
            else if (line.Contains(PartialMarker, StringComparison.Ordinal)) partial++;
            else if (line.Contains(MissingMarker, StringComparison.Ordinal)) missing++;
        }

        var total = covered + partial + missing;
        var percent = total == 0 ? 0d : Math.Round(100.0 * covered / total, 1);
        return new SkillCoverageReport(total, covered, partial, missing, percent, DateTime.UtcNow);
    }

    private static bool IsTableRow(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("| ") && !trimmed.StartsWith("| ---") && !trimmed.StartsWith("| #");
    }
}
