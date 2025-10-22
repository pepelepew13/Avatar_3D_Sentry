using System.Text;
using System.Security.Cryptography.X509Certificates;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Models;            // ApplicationUser, JwtSettings
using Avatar_3D_Sentry.Security;          // CompanyAccessRequirement/Handler
using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Services.Storage;  // IAssetStorage + impls
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ==================== Carga .env (opcional) ====================
try
{
    var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var envPath = Path.Combine(userHome, "Documents", "GitHub", ".env");
    if (File.Exists(envPath)) DotNetEnv.Env.Load(envPath);
    // También intenta .env del cwd (sin reventar si no está)
    DotNetEnv.Env.Load(require: false);
}
catch { /* ignore */ }

// ==================== Helper para "env:VAR" ====================
static string? ResolveEnv(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return value;
    const string prefix = "env:";
    if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        var key = value.Substring(prefix.Length);
        var env = Environment.GetEnvironmentVariable(key);
        return env;
    }
    return value;
}

// ==================== Certificado opcional ====================
var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
var certPassword = builder.Configuration["Kestrel:Certificates:Default:Password"];
if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificate = string.IsNullOrEmpty(certPassword)
                ? new X509Certificate2(certPath)
                : new X509Certificate2(certPath, certPassword);
        });
    });
}

// ==================== JWT Auth ====================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKeyRaw = jwtSection["Key"] ?? throw new InvalidOperationException("Config Jwt:Key requerido");
var jwtKey = ResolveEnv(jwtKeyRaw) ?? jwtKeyRaw;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("CanEditAvatar", p => p.Requirements.Add(new CompanyAccessRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, CompanyAccessHandler>();

// ==================== MVC + Swagger ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Avatar 3D Sentry", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT en el header: **Bearer {token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference{ Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            }, Array.Empty<string>()
        }
    });
});

// ==================== CORS (panel) ====================
var panelUrl = builder.Configuration.GetValue<string>("Dashboard:PanelUrl") ?? "http://localhost:5168";
const string dashboardCorsPolicy = "DashboardCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        dashboardCorsPolicy,
        policy => policy
            .WithOrigins(panelUrl)
            .WithMethods("GET","POST","PUT","DELETE","OPTIONS")
            .AllowAnyHeader()
            .AllowCredentials()
    );
});

// ==================== DbContext: SqlServer / MySql / Sqlite ====================
var provider = builder.Configuration.GetValue<string>("Database:Provider")?.Trim();
var rawConnString = builder.Configuration.GetConnectionString("AvatarDb");
var connString = ResolveEnv(rawConnString) ?? rawConnString;

builder.Services.AddDbContext<AvatarContext>(opt =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseSqlServer(connString!, sql =>
        {
            sql.EnableRetryOnFailure();
            sql.CommandTimeout(120);
        });
    }
    else if (string.Equals(provider, "MySql", StringComparison.OrdinalIgnoreCase))
    {
        var serverVersion = ServerVersion.AutoDetect(connString!);
        opt.UseMySql(connString!, serverVersion);
    }
    else
    {
        var sqliteBuilder = new SqliteConnectionStringBuilder(connString ?? "Data Source=Data/avatar-dev.db");
        if (!Path.IsPathRooted(sqliteBuilder.DataSource))
            sqliteBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteBuilder.DataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(sqliteBuilder.DataSource)!);
        opt.UseSqlite(sqliteBuilder.ConnectionString);
    }
});

// ==================== Opciones: Speech & Storage ====================
builder.Services.Configure<SpeechOptions>(opts =>
{
    var s = builder.Configuration.GetSection("Speech");
    opts.Key         = ResolveEnv(s["Key"])      ?? s["Key"];
    opts.Region      = ResolveEnv(s["Region"])   ?? s["Region"];
    opts.Endpoint    = ResolveEnv(s["Endpoint"]) ?? s["Endpoint"];
    opts.DefaultVoice= ResolveEnv(s["DefaultVoice"]) ?? s["DefaultVoice"] ?? "es-CO-SalomeNeural";
});

builder.Services.Configure<StorageOptions>(opts =>
{
    var s = builder.Configuration.GetSection("Storage");
    opts.Mode = s["Mode"] ?? "Auto";
    opts.AzureConnection = ResolveEnv(s["AzureConnection"]) ?? s["AzureConnection"];

    opts.Containers.Models      = ResolveEnv(s.GetSection("Containers")["Models"])      ?? "models";
    opts.Containers.Logos       = ResolveEnv(s.GetSection("Containers")["Logos"])       ?? "logos";
    opts.Containers.Backgrounds = ResolveEnv(s.GetSection("Containers")["Backgrounds"]) ?? "backgrounds";
    opts.Containers.Audio       = ResolveEnv(s.GetSection("Containers")["Audio"])       ?? "audio";

    if (int.TryParse(ResolveEnv(s["SasExpiryMinutes"]) ?? s["SasExpiryMinutes"], out var sasMin))
        opts.SasExpiryMinutes = sasMin;

    var local = s.GetSection("Local");
    opts.Local.Root         = local["Root"]         ?? "wwwroot";
    opts.Local.ModelsPath   = local["ModelsPath"]   ?? "wwwroot/models";
    opts.Local.LogosPath    = local["LogosPath"]    ?? "wwwroot/logos";
    opts.Local.BackgroundsPath = local["BackgroundsPath"] ?? "wwwroot/backgrounds";
    opts.Local.AudioPath    = local["AudioPath"]    ?? "Resources/audio";
});

// ==================== Servicios propios (TTS, Storage) ====================
builder.Services.AddSingleton<PhraseGenerator>();
builder.Services.AddSingleton<ITtsService, AzureTtsService>();

builder.Services.AddSingleton<IAssetStorage>(sp =>
{
    var so = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AssetStorage");

    if (!string.IsNullOrWhiteSpace(so.AzureConnection) &&
        !string.Equals(so.AzureConnection, "env:AZURE_STORAGE_CONNECTION", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation("Usando AzureBlobStorage.");
        return new AzureBlobStorage(so);
    }

    logger.LogInformation("Usando LocalFileStorage.");
    return new LocalFileStorage(env, so);
});

var app = builder.Build();

// ==================== Migrar DB + seed SUPERADMIN ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AvatarContext>();
    db.Database.Migrate();

    if (!db.Users.Any(u => u.Role == "SuperAdmin"))
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        var super = new ApplicationUser { Email = "superadmin@sentry.local", Role = "SuperAdmin" };
        super.PasswordHash = hasher.HashPassword(super, "Super#12345");
        db.Users.Add(super);
        await db.SaveChangesAsync();
    }
}

// ==================== Pipeline ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(dashboardCorsPolicy);

// Tipos para GLB/GLTF
var ctp = new FileExtensionContentTypeProvider();
ctp.Mappings[".glb"]  = "model/gltf-binary";
ctp.Mappings[".gltf"] = "model/gltf+json";

// Archivos estáticos para modelos (wwwroot/models)
var modelsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "models");
if (Directory.Exists(modelsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(modelsPath),
        RequestPath = "/models",
        ContentTypeProvider = ctp
    });
}

// Archivos estáticos para recursos (Resources/)
var resourcesPath = Path.Combine(app.Environment.ContentRootPath, "Resources");
if (Directory.Exists(resourcesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(resourcesPath),
        RequestPath = "/resources",
        ContentTypeProvider = ctp
    });
}

// Además sirve wwwroot completo
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

public partial class Program { }
