using AvatarAdmin.Components;    
using AvatarAdmin.Services;        
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// (Opcional) Cargar .env local para Admin
try
{
    var externalEnv = @"C:\Users\USUARIO\Documents\GitHub\.env";
    if (System.IO.File.Exists(externalEnv)) Env.Load(externalEnv);
}
catch { /* ignore */ }

// Base URL del backend
var apiBase = Environment.GetEnvironmentVariable("AVATARBACK_BASEURL")
              ?? builder.Configuration["Api:BaseUrl"]
              ?? "http://localhost:5216";

// Blazor Server (.NET 8)
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Estado de autenticación (JWT, claims)
builder.Services.AddScoped<AuthState>();

// Persistencia de sesión (localStorage) y handler de Bearer + 401
builder.Services.AddScoped<AuthPersistence>();
builder.Services.AddScoped<BearerHandler>();

// HttpClient tipado para la API del backend con handler (auto-Bearer + manejo 401)
builder.Services.AddHttpClient<AvatarApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
})
.AddHttpMessageHandler<BearerHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
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
