# Avatar_3D_Sentry
Sistema de gestion y personalización de agente 3D de sentry

## Certificados HTTPS

El repositorio no incluye certificados. Para habilitar HTTPS, coloca el archivo
`*.pfx` en una ubicación segura fuera del control de versiones y establece las
variables de entorno:

- `CERT_PATH`: ruta al archivo del certificado.
- `CERT_PASSWORD`: contraseña del certificado (opcional).

### Entorno local

```bash
export CERT_PATH=/ruta/al/certificado/aspnetapp.pfx
export CERT_PASSWORD=mi_contraseña  # si aplica
dotnet run
```

### Producción

Configura las mismas variables de entorno en el servidor y asegúrate de que el
archivo del certificado esté disponible en la ruta indicada.
