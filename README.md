# Avatar_3D_Sentry
Sistema de gestion y personalización de agente 3D de sentry

## Certificados HTTPS

El repositorio no incluye certificados. Para habilitar HTTPS, coloca el archivo
`*.pfx` en una ubicación segura fuera del control de versiones (por ejemplo,
`/etc/ssl/private/aspnetapp.pfx`) y establece las variables de entorno:

- `Kestrel__Certificates__Default__Path`: ruta al archivo del certificado.
- `Kestrel__Certificates__Default__Password`: contraseña del certificado (opcional).

### Entorno local

```bash
export Kestrel__Certificates__Default__Path=/ruta/externa/aspnetapp.pfx
export Kestrel__Certificates__Default__Password=mi_contraseña  # si aplica
dotnet run
```

### Producción

Configura las mismas variables de entorno en el servidor y asegúrate de que el
archivo del certificado esté disponible en la ruta indicada.

## Configurar Azure Speech (TTS)

La API usa Azure Speech para generar audio y visemas (lip-sync). Define las
variables de entorno antes de ejecutar la aplicación. No guardes las llaves en
el repositorio.

```bash
export SPEECH_KEY="<tu_speech_key>"
export SPEECH_REGION="eastus2"                    # o usa SPEECH_ENDPOINT si prefieres
export SPEECH_ENDPOINT="https://<tu_endpoint>.api.cognitive.microsoft.com/"  # opcional
export VOICE_NAME="es-CO-SalomeNeural"            # voz por defecto
dotnet run --project AvatarBack/Avatar_3D_Sentry.csproj
```

Si usas un archivo `.env` o `appsettings.Development.json` local (ambos están
excluidos del control de versiones), la sección `Speech` acepta los mismos
campos (`Key`, `Region`, `Endpoint`, `DefaultVoice`).

## Autenticación de la API

La API protege todos los endpoints mediante un token Bearer. El valor se
define en `appsettings.json` con la clave `TokenAutorizacion`. Para realizar
peticiones debes incluir el encabezado:

```
Authorization: Bearer <token-configurado>
```

Durante el desarrollo puedes deshabilitar temporalmente el middleware de
autenticación estableciendo `"RequerirToken": false` en
`appsettings.Development.json`. La aplicación registrará una advertencia en
tiempo de ejecución para recordarte que esta configuración solo debe usarse en
entornos locales.

## Persistencia de configuraciones del avatar

Las preferencias (logo, fondo, voz, idioma, etc.) se guardan en SQLite. Por
defecto el archivo se crea en `Data/avatar.db` dentro del directorio raíz del
proyecto. Si deseas almacenar la base en otra ubicación o usar un proveedor
diferente, edita la cadena de conexión `ConnectionStrings:AvatarDb` en
`appsettings.json` o sobrescribe el valor mediante variables de entorno.

En entornos de desarrollo se recomienda utilizar un archivo distinto (por
ejemplo, `Data/avatar-dev.db`). El archivo está excluido del control de
versiones, por lo que cada entorno mantiene su propia base.

## Logo de Sentry en el panel AvatarAdmin

El logotipo que aparece en la cabecera del panel no se versiona para evitar
subir binarios innecesarios. Copia manualmente el archivo proporcionado por el
equipo de diseño en la ruta `AvatarAdmin/wwwroot/img/Logo_Sentry.png` antes de
compilar. Si necesitas cambiarlo, reemplaza el archivo en esa misma ubicación.

## Modelos 3D del avatar

El repositorio solo contiene el código para cargar los modelos del avatar. Para
visualizarlos en el panel de administración debes copiar los archivos `.glb`
generados por el equipo 3D en la carpeta `wwwroot/models/` del proyecto
principal:

- `wwwroot/models/Avatar.glb` (uniforme corporativo predeterminado)
- `wwwroot/models/traje.glb` (versión con traje ejecutivo)
- `wwwroot/models/vestido.glb` (versión con vestido formal)

Al cambiar las prendas desde el panel, el visor 3D recargará automáticamente el
modelo correspondiente siempre que los archivos existan en esa ruta.

## Configuración del panel web

La URL del panel autorizado para consumir la API se controla mediante la clave
`Dashboard:PanelUrl` en `appsettings*.json`. Ajusta este valor para coincidir con
la dirección desde la que se hospeda el panel (por defecto `http://localhost:5168`).

## Snippet para el sistema de turnos del cliente

Si ellos ya muestran “turno / módulo” a la izquierda, a la derecha pueden incrustar un canvas y el script. Con una sola llamada a tu API, el avatar habla y mueve labios:


`<!-- En la página del cliente -->
<div style="width:100%;max-width:520px;margin:auto">
  <canvas id="avatar-canvas" style="width:100%;height:420px;border-radius:16px"></canvas>
  <audio id="avatar-audio" hidden preload="auto"></audio>
</div>

<script type="module">
import * as THREE from "https://unpkg.com/three@0.160.0/build/three.module.js";
window.THREE = THREE;
import "https://tu-panel-o-cdn/js/avatarViewer.js"; // sirve el que ya tienes

async function announceTurno(payload){
  // payload: { empresa, sede, modulo, turno, nombre }
  const res = await fetch("https://TU_API/api/avatar/announce?idioma=es", {
    method: "POST",
    headers: { "Content-Type":"application/json", "Authorization":"Bearer secret-token" },
    body: JSON.stringify(payload)
  });
  if(!res.ok) throw new Error("API error");
  return res.json();
}

(async () => {
  const canvas = document.getElementById("avatar-canvas");
  // inicializa viewer (modelo por defecto; el logo/fondo lo trae el admin)
  window.AvatarViewer.init(canvas, { modelUrl: "/models/Avatar.glb", background: "oficina" });

  // Ejemplo: cuando tu sistema llame un turno:
  const data = await announceTurno({
    empresa: "Sentry",
    sede: "Pereira",
    modulo: "Módulo 3",
    turno: "A015",
    nombre: "Juan Pérez"
  });

  // Preparar audio y visemas
  const audio = document.getElementById("avatar-audio");
  const durationMs = await window.AvatarViewer.prepareAudioClip(audio, data.audioUrl);
  window.AvatarViewer.applyVisemes(data.visemas);
  window.AvatarViewer.playTalking(durationMs);
  await window.AvatarViewer.playPreparedAudioClip(audio);
})();
</script>
`
