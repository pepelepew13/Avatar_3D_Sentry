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
