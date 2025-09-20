using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Middleware;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

var awsAccessKey = builder.Configuration["AWS:AccessKeyId"];
var awsSecretKey = builder.Configuration["AWS:SecretAccessKey"];
if (!string.IsNullOrWhiteSpace(awsAccessKey) && !string.IsNullOrWhiteSpace(awsSecretKey))
{
    builder.Services.AddSingleton<ITtsService, PollyTtsService>();
}
else
{
    builder.Services.AddSingleton<ITtsService, NullTtsService>();
}

var connectionString = builder.Configuration.GetConnectionString("AvatarDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("The connection string 'AvatarDatabase' was not found.");
}

var configuredProvider = builder.Configuration.GetValue<string>("Database:Provider");
builder.Services.AddDbContext<AvatarContext>(opt =>
{
    var provider = (configuredProvider ?? "Sqlite").Trim();

    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseSqlServer(connectionString);
        return;
    }

    if (provider.Length > 0 && !string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
    }

    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
    {
        throw new InvalidOperationException("The SQLite connection string must define a Data Source.");
    }

    var dataSourcePath = sqliteBuilder.DataSource;
    if (!Path.IsPathRooted(dataSourcePath))
    {
        dataSourcePath = Path.Combine(builder.Environment.ContentRootPath, dataSourcePath);
    }

    var dataDirectory = Path.GetDirectoryName(dataSourcePath);
    if (!string.IsNullOrEmpty(dataDirectory))
    {
        Directory.CreateDirectory(dataDirectory);
    }

    sqliteBuilder.DataSource = dataSourcePath;
    opt.UseSqlite(sqliteBuilder.ToString());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AvatarContext>();
    context.Database.Migrate();
}

// Configura la canalización de solicitudes HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseMiddleware<MiddlewareAutenticacionToken>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program {}
