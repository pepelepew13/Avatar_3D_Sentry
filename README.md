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

## Configurar AWS Polly

Para habilitar la síntesis de voz real con Amazon Polly debes proporcionar
credenciales válidas. **No** almacenes las llaves en el repositorio; usa variables
de entorno o `dotnet user-secrets` durante el desarrollo.

```bash
export AWS_ACCESS_KEY_ID="<tu_access_key>"
export AWS_SECRET_ACCESS_KEY="<tu_secret_key>"
export AWS_REGION="us-east-1"  # opcional, por defecto us-east-1
dotnet run
```

También puedes crear un archivo `appsettings.Development.json` local (excluido
del control de versiones) con la sección `AWS`:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "<tu_access_key>",
    "SecretAccessKey": "<tu_secret_key>"
  }
}
```

En entornos productivos se recomienda usar perfiles compartidos de AWS o un
servicio de gestión de secretos compatible.

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

