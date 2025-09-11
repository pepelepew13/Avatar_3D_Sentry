using Avatar_3D_Sentry.Services;

var builder = WebApplication.CreateBuilder(args);

// Agrega servicios al contenedor.

builder.Services.AddControllers();
// Aprende más sobre la configuración de Swagger/OpenAPI en https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PhraseGenerator>();
builder.Services.AddSingleton<ITtsService, PollyTtsService>();

var app = builder.Build();

// Configura la canalización de solicitudes HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
