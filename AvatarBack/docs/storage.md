# Almacenamiento de assets (Azure Blob Storage)

## Cuenta y contenedores
- **Cuenta**: `stavatarsentryprod`
- **Endpoint**: `https://stavatarsentryprod.blob.core.windows.net/`
- **Contenedores**:
  - `public`: recursos públicos (branding y modelos)
  - `tts`: audios generados por texto a voz

## Patrones de rutas
- Branding (logos, fondos): `public/{empresa}/{sede}/branding/<archivo>`
- Modelos GLB: `public/{empresa}/{sede}/models/<archivo>`
- Audio TTS: `tts/{empresa}/{sede}/{yyyy}/{MM}/{dd}/<id>.mp3`

La API sigue usando alias lógicos (`logos`, `backgrounds`, `models`, `audio`) para construir estas rutas; internamente se mapean a los contenedores anteriores.

## Acceso
- Por ahora los clientes obtienen **SAS de lectura** generados por el backend.
- Cuando el backend se ejecute en **App Service Linux (.NET 8)** se podrá habilitar **Managed Identity** y dar permisos RBAC directos a los contenedores, eliminando la necesidad de keys o SAS generadas manualmente.
