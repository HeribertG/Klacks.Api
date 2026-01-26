using FluentValidation;
using Klacks.Api;
using Klacks.Api.Application.Validation;
using Klacks.Api.Data.Seed;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Converters;
using Klacks.Api.Infrastructure.Exceptions;
using Klacks.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Mappers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(jwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

var openRouteServiceSettings = new Klacks.Api.Domain.Models.Settings.OpenRouteServiceSettings();
builder.Configuration.Bind("OpenRouteService", openRouteServiceSettings);
builder.Services.AddSingleton(openRouteServiceSettings);

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
        ValidateActor = false,
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
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
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
                              origins.Add(corsAdditional);
                          }
                          cors.WithOrigins(origins.ToArray())
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders(headers)
                            .AllowCredentials();
                      });
});

builder.Services.AddApplicationServices();

builder.Services.AddSignalR();
builder.Services.AddScoped<IWorkNotificationService, WorkNotificationService>();
builder.Services.AddScoped<IShiftStatsNotificationService, ShiftStatsNotificationService>();
builder.Services.AddSingleton<PeriodHoursBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PeriodHoursBackgroundService>());
builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

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

builder.Services
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

builder.Services.AddIdentity<AppUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<DataBaseContext>()
        .AddDefaultTokenProviders();

// Registering Database
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseNpgsql(dataSource);
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

Console.WriteLine("Version {0}", new MyVersion().Get(true));

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

app.UseRouting();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
    {
        endpoints.MapRazorPages();
        endpoints.MapBlazorHub();
        endpoints.MapControllers();
        endpoints.MapHub<WorkNotificationHub>("/hubs/work-notifications");
        endpoints.MapHealthChecks("/health");
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

app.Run();
