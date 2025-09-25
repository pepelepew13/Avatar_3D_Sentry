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
