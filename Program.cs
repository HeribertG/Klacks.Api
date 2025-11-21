using FluentValidation;
using Klacks.Api;
using Klacks.Api.Application.Validation;
using Klacks.Api.Data.Seed;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Converters;
using Klacks.Api.Infrastructure.Exceptions;
using Klacks.Api.Infrastructure.Extensions;
using Klacks.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
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
FakeSettings.WithFake = builder.Configuration["Fake:WithFake"] ?? string.Empty;
FakeSettings.ClientsNumber = builder.Configuration["Fake:ClientNumber"] ?? string.Empty;
FakeSettings.MaxBreaksPerClientPerYear = builder.Configuration["Fake:MaxBreaksPerClientPerYear"] ?? "30";

var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(jwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Klacks-Net API",
        Version = "v1",
        Description = "API für Klacks-Net Anwendung"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.ValidIssuer,
        ValidAudience = jwtSettings.ValidAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
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
                          cors.WithOrigins(corsHost!, corsHome!)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders(headers)
                            .AllowCredentials();
                      });
});

builder.Services.AddApplicationServices();

builder.Services.AddMemoryCache();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MVC Views
builder.Services.AddControllersWithViews();

// Add Domain Services
builder.Services.AddScoped<Klacks.Api.Domain.Interfaces.IPasswordGeneratorService, Klacks.Api.Domain.Services.Accounts.PasswordGeneratorService>();
builder.Services.AddScoped<Klacks.Api.Domain.Services.ContainerTemplates.ContainerTemplateService>();

// Add Geocoding Service
builder.Services.AddHttpClient("Nominatim");
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Klacks.Api.Infrastructure.Services.IGeocodingService, Klacks.Api.Infrastructure.Services.GeocodingService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
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
builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(warnings => 
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Registering Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Registering Mediator
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

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
    app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0);
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Klacks-Net API V1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
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
