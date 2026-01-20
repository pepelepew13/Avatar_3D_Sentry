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
// 3) CONSUMO API INTERNA (OBLIGATORIO)
// ==================================================================
if (string.IsNullOrWhiteSpace(internalApiOptions.BaseUrl))
    throw new InvalidOperationException("Falta InternalApi:BaseUrl (debe venir desde .env o appsettings).");

builder.Services.AddHttpClient<InternalApiAvatarDataStore>(client =>
{
    client.BaseAddress = new Uri(internalApiOptions.BaseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<IAvatarDataStore, InternalApiAvatarDataStore>();
builder.Services.AddHttpClient<IInternalApiTokenService, InternalApiTokenService>();
builder.Services.AddTransient<InternalApiAuthHandler>();
builder.Services.AddHttpClient<IInternalUserClient, InternalUserClient>()
    .AddHttpMessageHandler<InternalApiAuthHandler>();
builder.Services.AddHttpClient<IInternalAvatarConfigClient, InternalAvatarConfigClient>()
    .AddHttpMessageHandler<InternalApiAuthHandler>();

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
        Containers = new StorageOptions.ContainerNames
        {
            // Tu AzureStorageOptions solo tiene public y tts,
            // así que mapeamos varios a public y audio a tts:
            Models = az.ContainerNamePublic,
            Logos = az.ContainerNamePublic,
            Backgrounds = az.ContainerNamePublic,
            Videos = az.ContainerNameVideos,
            Audio = az.ContainerNameTts
        }
        // SasExpiryMinutes / AudioRetentionDays / Local quedan por default
    };

    if (!string.IsNullOrWhiteSpace(storage.AzureConnection))
        return new AzureBlobStorage(storage);

    throw new InvalidOperationException("AzureStorage:ConnectionString vacío. Storage local no configurado.");
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
