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

## AvatarAdmin

Para preparar la base de datos y ejecutar la aplicación de administración, puede
utilizar el siguiente comando:

```bash
cd AvatarAdmin && dotnet ef database update && dotnet run
```

### Prueba manual: carga de configuración desde la UI

1. Inicie la aplicación de administración con el comando anterior y abra
   `https://localhost:5001` (o el puerto configurado) en el navegador.
2. En el panel principal, complete los campos **Empresa** y **Sede** con valores
   válidos para su entorno y presione **Cargar configuración**.
3. Verifique en las herramientas de desarrollo del navegador que la petición a
   `GET /api/avatar-config/{empresa}/{sede}` se complete correctamente y que el
   formulario muestre los datos recuperados (logo, vestimenta, idioma, voz y
   fondo).
4. Confirme que no aparece ningún mensaje de error ni se registran excepciones
   en la consola del navegador o en los logs del servidor; esto valida que la
   acción `LoadConfigAsync` se ejecuta sin ser rechazada por el runtime.
