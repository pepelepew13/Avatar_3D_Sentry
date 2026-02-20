using System.Text;
using Avatar_3D_Sentry.Security;
using Avatar_3D_Sentry.Settings;
using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Services.Storage;
using Avatar_3D_Sentry.Swagger;
using AvatarSentry.Application.Config;
using AvatarSentry.Application.InternalApi;
using AvatarSentry.Application.InternalApi.Clients;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1) CARGA DE VARIABLES DE ENTORNO (.env externo)
// ==================================================================
var envPath =
    Environment.GetEnvironmentVariable("SENTRY_ENV_PATH")
    ?? @"C:\Users\USUARIO\OneDrive\Escritorio\Sentry\Credenciales\.env";

if (File.Exists(envPath))
{
    // Carga variables al proceso (Environment)
    Env.Load(envPath);

    // Re-agrega provider para que IConfiguration lea las env vars recién cargadas
    builder.Configuration.AddEnvironmentVariables();

    Console.WriteLine($"✅ .env cargado desde: {envPath}");
}
else
{
    Console.WriteLine($"⚠️ ADVERTENCIA: No se encontró el archivo .env en: {envPath}");
}

// ==================================================================
// 2) CONFIGURACIÓN TIPADA (Options Pattern)
// ==================================================================
builder.Services.Configure<AzureSpeechOptions>(builder.Configuration.GetSection("AzureSpeech"));
builder.Services.Configure<AzureStorageOptions>(builder.Configuration.GetSection("AzureStorage"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection("InternalApi"));
builder.Services.Configure<InternalApiSettings>(builder.Configuration.GetSection("InternalApi"));
builder.Services.Configure<PublicApiOptions>(builder.Configuration.GetSection(PublicApiOptions.SectionName));

var internalApiOptions = builder.Configuration.GetSection(InternalApiOptions.SectionName).Get<InternalApiOptions>()
    ?? new InternalApiOptions();

// ==================================================================
// 3) CONSUMO API INTERNA (opcional hasta que Sentry despliegue la API que conecta la DB)
// ==================================================================
// Según documento MetaFusion→Sentry: la API interna y la DB son responsabilidad de Sentry.
// Este BFF solo consume /internal/* (companies, sites, users, avatar-config, voices). Cuando BaseUrl esté configurado, se usan los clientes HTTP.
if (!string.IsNullOrWhiteSpace(internalApiOptions.BaseUrl))
{
    builder.Services.AddHttpClient<InternalApiAvatarDataStore>(client =>
    {
        client.BaseAddress = new Uri(internalApiOptions.BaseUrl.TrimEnd('/') + "/");
    }).AddHttpMessageHandler<InternalApiApiKeyHandler>();
    builder.Services.AddScoped<IAvatarDataStore, InternalApiAvatarDataStore>();
    builder.Services.AddHttpClient<IInternalApiTokenService, InternalApiTokenService>()
        .AddHttpMessageHandler<InternalApiApiKeyHandler>();
    builder.Services.AddTransient<InternalApiAuthHandler>();
    builder.Services.AddHttpClient<IInternalUserClient, InternalUserClient>()
        .AddHttpMessageHandler<InternalApiAuthHandler>();
    builder.Services.AddHttpClient<IInternalAvatarConfigClient, InternalAvatarConfigClient>()
        .AddHttpMessageHandler<InternalApiAuthHandler>();
    builder.Services.AddHttpClient<IInternalCompanyClient, InternalCompanyClient>()
        .AddHttpMessageHandler<InternalApiAuthHandler>();
    builder.Services.AddHttpClient<IInternalSiteClient, InternalSiteClient>()
        .AddHttpMessageHandler<InternalApiAuthHandler>();
    builder.Services.AddHttpClient<IInternalKpisClient, InternalKpisClient>()
        .AddHttpMessageHandler<InternalApiAuthHandler>();
    builder.Services.AddScoped<ICompanySiteResolutionService, CompanySiteResolutionService>();
}
else
{
    builder.Services.AddSingleton<IAvatarDataStore, StubAvatarDataStore>();
    builder.Services.AddSingleton<IInternalUserClient, StubInternalUserClient>();
    builder.Services.AddSingleton<IInternalAvatarConfigClient, StubInternalAvatarConfigClient>();
    builder.Services.AddSingleton<IInternalCompanyClient, StubInternalCompanyClient>();
    builder.Services.AddSingleton<IInternalSiteClient, StubInternalSiteClient>();
    builder.Services.AddSingleton<IInternalKpisClient, StubInternalKpisClient>();
    builder.Services.AddSingleton<ICompanySiteResolutionService, StubCompanySiteResolutionService>();
}

// ==================================================================
// 4) JWT AUTH
// ==================================================================
var jwtOptions = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("Falta configuración JwtSettings.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    throw new InvalidOperationException("Falta JwtSettings:Key (debe venir desde .env).");

var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Key);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

// (Opcional, pero recomendado si tienes [Authorize])
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditAvatar", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "User");
        policy.AddRequirements(new CompanyAccessRequirement());
    });
});

// ==================================================================
// 5) SERVICIOS (Controllers, Swagger, TTS, Storage)
// ==================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IAuthorizationHandler, CompanyAccessHandler>();
builder.Services.AddScoped<ICompanyAccessService, CompanyAccessService>();
builder.Services.AddSingleton<PhraseGenerator>();
builder.Services.AddTransient<InternalApiApiKeyHandler>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Avatar Sentry API", Version = "v1" });
    c.CustomSchemaIds(type => type.FullName);
    c.OperationFilter<FileUploadOperationFilter>();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en formato: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Servicio TTS
builder.Services.AddSingleton<ITtsService, AzureTtsService>();

// Storage: tu AzureBlobStorage espera StorageOptions (no AzureStorageOptions)
// => hacemos el mapeo AzureStorageOptions -> StorageOptions aquí
builder.Services.AddSingleton<IAssetStorage>(sp =>
{
    // 1) Lees AzureStorage (desde .env / appsettings)
    var az = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;

    // 2) Adaptas a StorageOptions (que es lo que tu AzureBlobStorage espera)
    var storage = new StorageOptions
    {
        Mode = "Azure",
        AzureConnection = az.ConnectionString,
        SasExpiryMinutes = az.SasExpiryMinutes,
        Containers = new StorageOptions.ContainerNames
        {
            Models = az.ContainerNamePublic,
            Logos = az.ContainerNamePublic,
            Backgrounds = az.ContainerNamePublic,
            Videos = az.ContainerNameVideos,
            Audio = az.ContainerNameTts
        }
    };

    // Si hay connection string, usa Azure
    if (!string.IsNullOrWhiteSpace(storage.AzureConnection))
    {
        return new AzureBlobStorage(storage);
    }

    // Si no hay connection string, usa Local
    storage.Mode = "Local";
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new LocalFileStorage(env, storage); // ✅ ahora sí
});

// ==================================================================
// 6) CORS (panel admin)
// ==================================================================
var allowedOrigin = builder.Configuration["Dashboard:PanelUrl"] ?? "http://localhost:5168";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
    );
});

var app = builder.Build();

// ==================================================================
// 7) PIPELINE
// ==================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowDashboard");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
