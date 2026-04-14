// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Validation;
using Klacks.Api.Data.Seed;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Converters;
using Klacks.Api.Infrastructure.Exceptions;
using Klacks.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Middleware;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.StartupChecks;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Klacksy;
using Klacks.Api.Infrastructure.Repositories.Klacksy;
using Klacks.Api.Application.Mappers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var myAllowSpecificOrigins = "CorsPolicy";
string[] headers =["X-Operation", "X-Resource", "X-Total-Count"];

var builder = WebApplication.CreateBuilder(args);

// Konfigurieren des Loggings
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var corsHost = builder.Configuration["Cors:Host"];
var corsHome = builder.Configuration["Cors:Home"];
var corsAdditional = builder.Configuration["Cors:Additional"];
FakeSettings.WithFake = builder.Configuration["Fake:WithFake"] ?? string.Empty;
FakeSettings.ClientsNumber = builder.Configuration["Fake:ClientNumber"] ?? string.Empty;
FakeSettings.MaxBreaksPerClientPerYear = builder.Configuration["Fake:MaxBreaksPerClientPerYear"] ?? "30";
FakeSettings.UseDumpFile = builder.Configuration.GetValue("Fake:UseDumpFile", true);

var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(jwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

var openRouteServiceSettings = new Klacks.Api.Domain.Models.Settings.OpenRouteServiceSettings();
builder.Configuration.Bind("OpenRouteService", openRouteServiceSettings);
builder.Services.AddSingleton(openRouteServiceSettings);

builder.Services.Configure<Klacks.Api.Domain.Models.Settings.PasswordResetSettings>(
    builder.Configuration.GetSection("PasswordReset"));

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Klacks-Net API";
        document.Info.Version = "v1";
        document.Info.Description = "API für Klacks-Net Anwendung";
        return Task.CompletedTask;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = false, // Actor claim not used in this application
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.ValidIssuer,
        ValidAudience = jwtSettings.ValidAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;
            var accessToken = context.Request.Query["access_token"];
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");

            var isSttPath = path.StartsWithSegments("/api/backend/assistant/stt/stream");
            logger.LogDebug("OnMessageReceived: Path={Path}, IsHubPath={IsHubPath}, IsSttPath={IsSttPath}", path, path.StartsWithSegments("/hubs"), isSttPath);

            if (path.StartsWithSegments("/hubs"))
            {
                if (context.HttpContext.Items.TryGetValue("SignalRToken", out var signalRToken) && signalRToken is string tokenFromItems)
                {
                    context.Token = tokenFromItems;
                    logger.LogDebug("Token set from SignalR middleware (length: {TokenLength})", context.Token?.Length);
                }
                else if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = Uri.UnescapeDataString(accessToken);
                    logger.LogDebug("Token set from query string (length: {TokenLength})", context.Token?.Length);
                }
                else if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    logger.LogDebug("Token set from Authorization header (length: {TokenLength})", context.Token?.Length);
                }
                else
                {
                    logger.LogWarning("No token found for hub path. QueryToken={HasQueryToken}, AuthHeader={HasAuthHeader}",
                        !string.IsNullOrEmpty(accessToken), !string.IsNullOrEmpty(authHeader));
                }
            }
            else if (isSttPath && !string.IsNullOrEmpty(accessToken))
            {
                context.Token = Uri.UnescapeDataString(accessToken);
                logger.LogDebug("Token set from query string for STT WebSocket (length: {TokenLength})", context.Token?.Length);
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
            logger.LogWarning("Authentication failed: Path={Path}, Error={Error}", context.HttpContext.Request.Path, context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
            logger.LogDebug("OnChallenge: Path={Path}, Error={Error}, ErrorDescription={ErrorDescription}",
                context.HttpContext.Request.Path, context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
                      name: myAllowSpecificOrigins,
                      cors =>
                      {
                          var origins = new List<string> { corsHost!, corsHome! };
                          if (!string.IsNullOrEmpty(corsAdditional))
                          {
                              origins.AddRange(corsAdditional.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                          }
                          cors.WithOrigins(origins.ToArray())
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders(headers)
                            .AllowCredentials();
                      });
});

var bgOptions = builder.Configuration
    .GetSection(BackgroundServiceOptions.SectionName)
    .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

Klacks.Api.Infrastructure.Extensions.ServiceCollectionExtensions.RegisterPlugin(new Klacks.Plugin.Messaging.MessagingPluginRegistrar());
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IConnectionDateRangeTracker, ConnectionDateRangeTracker>();
builder.Services.AddScoped<IWorkNotificationService, WorkNotificationService>();
builder.Services.AddScoped<IShiftStatsNotificationService, ShiftStatsNotificationService>();
builder.Services.AddSingleton<PeriodHoursBackgroundService>();
if (bgOptions.PeriodHours)
    builder.Services.AddHostedService(sp => sp.GetRequiredService<PeriodHoursBackgroundService>());
builder.Services.AddSingleton<IScheduleTimelineStore, ScheduleTimelineStore>();
builder.Services.AddSingleton<ScheduleTimelineBackgroundService>();
if (bgOptions.ScheduleTimeline)
    builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduleTimelineBackgroundService>());
builder.Services.AddSingleton<IScheduleTimelineService>(sp => sp.GetRequiredService<ScheduleTimelineBackgroundService>());
builder.Services.AddSingleton<IUtteranceNormalizer, UtteranceNormalizer>();
builder.Services.AddSingleton<INavigationTargetCacheService>(sp =>
{
    var baseDir = AppContext.BaseDirectory;
    var manifest = Path.Combine(baseDir, "Application", "Skills", "Definitions", "navigation-targets.json");
    var plugins = Path.Combine(baseDir, "Plugins", "Languages");
    return new NavigationTargetCacheService(manifest, Directory.Exists(plugins) ? plugins : null);
});
builder.Services.AddScoped<INavigationTargetMatcher, NavigationTargetMatcher>();
builder.Services.AddScoped<IKlacksyNavigationFeedbackRepository, KlacksyNavigationFeedbackRepository>();
builder.Services.AddScoped<INavigationFeedbackLogger, NavigationFeedbackLogger>();
builder.Services.AddSingleton<LLMMapper>();
builder.Services.AddSingleton<IAssistantConnectionTracker, AssistantConnectionTracker>();
builder.Services.AddScoped<IAssistantNotificationService, AssistantNotificationService>();
builder.Services.AddScoped<Klacks.Api.Domain.Interfaces.Email.IEmailNotificationService, EmailNotificationService>();
builder.Services.AddSingleton<HeartbeatBackgroundService>();
if (bgOptions.Heartbeat)
    builder.Services.AddHostedService(sp => sp.GetRequiredService<HeartbeatBackgroundService>());
if (bgOptions.DataRetention)
    builder.Services.AddHostedService<DataRetentionBackgroundService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks()
    .AddCheck<PlatformDependencyHealthCheck>("platform-dependencies", tags: ["deep"]);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(RateLimitingPolicies.Login, opt =>
    {
        opt.PermitLimit = RateLimitingPolicies.LoginPermitLimit;
        opt.Window = RateLimitingPolicies.DefaultWindow;
    });

    options.AddFixedWindowLimiter(RateLimitingPolicies.Upload, opt =>
    {
        opt.PermitLimit = RateLimitingPolicies.UploadPermitLimit;
        opt.Window = RateLimitingPolicies.DefaultWindow;
    });

    options.AddFixedWindowLimiter(RateLimitingPolicies.RefreshToken, opt =>
    {
        opt.PermitLimit = RateLimitingPolicies.RefreshTokenPermitLimit;
        opt.Window = RateLimitingPolicies.DefaultWindow;
    });
});

builder.Services.AddPipelineBehavior(typeof(CancellationBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(ValidationBehavior<,>));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MVC Views
builder.Services.AddControllersWithViews();

// Add Data Protection for encrypting sensitive settings
var dataProtectionKeysPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Klacks", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .SetApplicationName("Klacks")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var mvcBuilder = builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        opts.JsonSerializerOptions.Converters.Add(new DateOnlyNullableJsonConverter());
        opts.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.WriteIndented = true;
    });

foreach (var asm in Klacks.Api.Infrastructure.Extensions.ServiceCollectionExtensions.GetPluginRegistrars().SelectMany(r => r.GetControllerAssemblies()))
{
    mvcBuilder.AddApplicationPart(asm);
}

builder.Services.AddIdentity<AppUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<DataBaseContext>()
        .AddDefaultTokenProviders();

// Registering Database
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
if (!connectionString.Contains("Command Timeout", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";Command Timeout=60;Timeout=30;Maximum Pool Size=200;";
}
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Registering Mappers (Mapperly)
builder.Services.AddMappers();

// Registering Mediator
builder.Services.AddMediator(Assembly.GetExecutingAssembly());

// Registering Database Initializer
builder.Services.AddDatabaseInitializer();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = true;
});

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

Console.WriteLine("Version {0}", MyVersion.Get(true));

NativeLibraryCheck.Verify();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Klacks-Net API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
    app.UseDeveloperExceptionPage();
}

// global cors policy
app.UseCors(myAllowSpecificOrigins);

app.UseHttpsRedirection();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseWebSockets();

// SignalR auth middleware must be before UseRouting and UseAuthentication
app.UseMiddleware<SignalRQueryStringAuthMiddleware>();

app.UseRouting();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.UseEndpoints(endpoints =>
    {
        endpoints.MapRazorPages();
        endpoints.MapBlazorHub();
        endpoints.MapControllers();
        endpoints.MapHub<WorkNotificationHub>("/hubs/work-notifications");
        endpoints.MapHub<AssistantNotificationHub>(SignalRConstants.AssistantHubPath);
        endpoints.MapHub<EmailNotificationHub>(SignalRConstants.EmailHubPath);
        endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => !check.Tags.Contains("deep")
        });
        endpoints.MapHealthChecks("/health/deep", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("deep"),
            ResponseWriter = WriteDeepHealthResponse
        }).RequireAuthorization();
    }
);

// Optional: Datenbank automatisch initialisieren beim Start
// Kann über appsettings.json gesteuert werden
if (builder.Configuration.GetValue<bool>("Database:InitializeOnStartup", false))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await dbInitializer.InitializeAsync();
    }
}

// Initialize Language Plugins + Skill Seeds parallel (independent operations)
await Task.WhenAll(
    app.InitializeLanguagePluginsAsync(),
    app.InitializeFeaturePluginsAsync(),
    app.LoadSkillSeedsAsync(),
    app.SeedGlobalAgentRulesAsync(),
    app.SeedAgentSoulSectionsAsync(),
    app.SeedUiControlsAsync(),
    app.SeedEmailFoldersAsync(),
    app.SeedSentimentKeywordsAsync());

// Skill Registry depends on LoadSkillSeeds being complete
await app.InitializeSkillRegistryAsync();

app.Run();

static async Task WriteDeepHealthResponse(HttpContext httpContext, HealthReport report)
{
    httpContext.Response.ContentType = "application/json";

    var checks = new Dictionary<string, string>();
    foreach (var entry in report.Entries)
    {
        foreach (var item in entry.Value.Data)
        {
            checks[item.Key] = item.Value?.ToString() ?? "Unknown";
        }
    }

    var response = new
    {
        status = report.Status.ToString(),
        checks
    };

    await httpContext.Response.WriteAsJsonAsync(response);
}
