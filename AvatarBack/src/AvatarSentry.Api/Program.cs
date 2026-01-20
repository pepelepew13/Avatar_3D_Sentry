using AvatarSentry.Application.Interfaces;
using AvatarSentry.Application.Settings;
using AvatarSentry.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<InternalApiSettings>(builder.Configuration.GetSection(InternalApiSettings.SectionName));
builder.Services.Configure<AzureStorageSettings>(builder.Configuration.GetSection(AzureStorageSettings.SectionName));
builder.Services.Configure<AzureSpeechSettings>(builder.Configuration.GetSection(AzureSpeechSettings.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Avatar Sentry API", Version = "v1" });
});

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
