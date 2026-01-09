using AvatarAdmin.Components;
using AvatarAdmin.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1) CARGA DE .env EXTERNO (igual que el backend)
// ==================================================================
var envPath =
    Environment.GetEnvironmentVariable("SENTRY_ENV_PATH")
    ?? @"C:\Users\USUARIO\OneDrive\Escritorio\Sentry\Credenciales\.env";

if (System.IO.File.Exists(envPath))
{
    Env.Load(envPath);
    builder.Configuration.AddEnvironmentVariables();
    Console.WriteLine($"✅ Admin: .env cargado desde: {envPath}");
}
else
{
    Console.WriteLine($"⚠️ Admin: no se encontró .env en: {envPath}");
}

// ==================================================================
// 2) BASE URL DE LA API (env > appsettings > default)
// ==================================================================
var apiBase =
    builder.Configuration["Api:BaseUrl"]
    ?? builder.Configuration["AVATARBACK_BASEURL"]
    ?? "http://localhost:5216";

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

// Estado / auth
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<AuthPersistence>();
builder.Services.AddScoped<BearerHandler>();

// HttpClient tipado al backend
builder.Services.AddHttpClient<AvatarApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
})
.AddHttpMessageHandler<BearerHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
