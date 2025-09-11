using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Middleware;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddSingleton<ITtsService, PollyTtsService>();
builder.Services.AddDbContext<AvatarContext>(opt =>
    opt.UseInMemoryDatabase("AvatarDb"));

var app = builder.Build();

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