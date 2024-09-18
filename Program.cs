using FluentValidation;
using FluentValidation.AspNetCore;
using Klacks_api;
using Klacks_api.BasicScriptInterpreter;
using Klacks_api.Converters;
using Klacks_api.Data.Seed;
using Klacks_api.Datas;
using Klacks_api.Helper;
using Klacks_api.Interfaces;
using Klacks_api.Models.Authentification;
using Klacks_api.Repositories;
using Klacks_api.Services;
using Klacks_api.Validation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var myAllowSpecificOrigins = "CorsPolicy";
string[] headers = ["X-Operation", "X-Resource", "X-Total-Count"];

var builder = WebApplication.CreateBuilder(args);

// Konfigurieren des Loggings
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var corsHost = builder.Configuration["Cors:Host"];
var corsHome = builder.Configuration["Cors:Home"];
FakeSettings.WithFake = builder.Configuration["Fake:WithFake"] ?? string.Empty;
FakeSettings.ClientsNumber = builder.Configuration["Fake:ClientNumber"] ?? string.Empty;

var jwtSettings = new JwtSettings();
builder.Configuration.Bind(nameof(jwtSettings), jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Klacks-Net API",
        Version = "v1",
        Description = "API für Klacks-Net Anwendung"
    });

    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        Description = "Bearer authentication with JWT Token",
        Type = SecuritySchemeType.Http,
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme,
                },
            },
            new List<string>()
        },
    });

    // XML Dokumentation einbeziehen
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Konfliktlösung für Aktionen
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // Behandlung von Datei-Uploads
    options.OperationFilter<FileUploadOperationFilter>();
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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IAnnotationRepository, AnnotationRepository>();
builder.Services.AddScoped<ICommunicationRepository, CommunicationRepository>();
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IAbsenceRepository, AbsenceRepository>();
builder.Services.AddScoped<IBreakRepository, BreakRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<ICalendarSelectionRepository, CalendarSelectionRepository>();
builder.Services.AddScoped<ISelectedCalendarRepository, SelectedCalendarRepository>();
builder.Services.AddScoped<IWorkRepository, WorkRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddSingleton<IMacroEngine, MacroEngine>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<UploadFile>();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services
    .AddFluentValidation(config =>
    {
        config.AutomaticValidationEnabled = true;  // Aktiviert automatische Validierung
    })
    .AddValidatorsFromAssemblyContaining<Klacks_api.Validation.CalendarSelections.PostCommandValidator>()
    .AddValidatorsFromAssemblyContaining<Klacks_api.Validation.CalendarSelections.PutCommandValidator>()
    .AddValidatorsFromAssemblyContaining<Klacks_api.Validation.Groups.PostCommandValidator>()
    .AddValidatorsFromAssemblyContaining<Klacks_api.Validation.Groups.PutCommandValidator>()
    .AddValidatorsFromAssemblyContaining<Klacks_api.Validation.Clients.GetTruncatedListQueryValidator>()
    .AddValidatorsFromAssemblyContaining<Klacks_api.Validation.Clients.FilterResourceValidator>();


builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

builder.Services.AddControllers()
       .AddJsonOptions(options =>
       {
           options.JsonSerializerOptions.WriteIndented = true;
           options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
       });

builder.Services.AddIdentity<AppUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<DataBaseContext>()
        .AddDefaultTokenProviders();

// Registering Database
string connectionString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<DataBaseContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Registering Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Registering Mediator
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

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

Console.WriteLine("UpdateDatabase start");
_ = new MyMigration(builder.Configuration, app.Services.GetRequiredService<ILoggerFactory>());
Console.WriteLine("UpdateDatabase done");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My Klacks-Net API V1");
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// global cors policy
app.UseCors(x => x
  .AllowAnyMethod()
  .AllowAnyHeader()
  .SetIsOriginAllowed(origin => true) // allow any origin
  .AllowCredentials()); // allow credentials

app.UseHttpsRedirection();

app.UseRouting();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapFallbackToFile("/index.html");
    endpoints.MapControllers();
}
);

app.Run();
