using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Middleware;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography.X509Certificates;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

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

// Agrega servicios al contenedor.

builder.Services.AddControllers();
// Aprende más sobre la configuración de Swagger/OpenAPI en https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token de autorización en formato 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<PhraseGenerator>();

builder.Services.AddSingleton<ITtsService>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("TtsInitialization");
    try
    {
        return ActivatorUtilities.CreateInstance<PollyTtsService>(sp);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "No se pudieron cargar credenciales de AWS Polly. Se usará NullTtsService.");
        return new NullTtsService();
    }
});
var connectionString = builder.Configuration.GetConnectionString("AvatarDb");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
    var dataSource = sqliteBuilder.DataSource;

    if (!Path.IsPathRooted(dataSource))
    {
        var fullPath = Path.Combine(builder.Environment.ContentRootPath, dataSource);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        sqliteBuilder.DataSource = fullPath;
    }
    else
    {
        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    builder.Services.AddDbContext<AvatarContext>(opt =>
        opt.UseSqlite(sqliteBuilder.ConnectionString));
}
else
{
    builder.Services.AddDbContext<AvatarContext>(opt =>
        opt.UseInMemoryDatabase("AvatarDb"));
}

var requerirToken = builder.Configuration.GetValue("RequerirToken", true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AvatarContext>();
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        db.Database.Migrate();
    }
}

// Configura la canalización de solicitudes HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
var resourcesPath = Path.Combine(app.Environment.ContentRootPath, "Resources");
if (Directory.Exists(resourcesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(resourcesPath),
        RequestPath = "/resources"
    });
}
app.UseHttpsRedirection();
if (requerirToken)
{
    app.UseMiddleware<MiddlewareAutenticacionToken>();
}
else
{
    app.Logger.LogWarning("La autenticación por token está deshabilitada según la configuración actual. No utilices este modo en producción.");
}
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program {}
