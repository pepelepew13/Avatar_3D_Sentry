using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("[controller]")]
public class AvatarController : ControllerBase
{
    private static readonly string[] Plantillas =
    [
        "Bienvenido {nombre} a {empresa}, sede {sede}, módulo {modulo}, turno {turno}.",
        "Hola {nombre}, {empresa} te espera en la sede {sede}, módulo {modulo}, turno {turno}.",
        "{nombre}, por favor dirígete a {empresa} - {sede}, módulo {modulo}, turno {turno}."
    ];

    [HttpPost("anunciar")]
    public IActionResult Anunciar([FromBody] SolicitudAnuncio solicitud)
    {
        var plantilla = Plantillas[Random.Shared.Next(Plantillas.Length)];
        var texto = plantilla
            .Replace("{empresa}", solicitud.Empresa)
            .Replace("{sede}", solicitud.Sede)
            .Replace("{modulo}", solicitud.Modulo)
            .Replace("{turno}", solicitud.Turno)
            .Replace("{nombre}", solicitud.Nombre);

        return Ok(new { texto });
    }
}
