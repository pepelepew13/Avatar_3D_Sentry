# Assets Sentry (diseños Stitch)

Carpeta para **imágenes e iconos** exportados del proyecto **Sentry Admin Dashboard** (Stitch, ID: `901339848894182002`).

## Cómo obtener los assets

En este entorno no está disponible el servidor MCP de Stitch (`get_project` / `get_screen`). Para poblar esta carpeta:

1. **Con Stitch MCP configurado en Cursor**  
   Ejecuta en otro agente o sesión que tenga el MCP:
   - `get_project` con el ID del proyecto.
   - `get_screen` para cada una de las 19 pantallas (IDs en `docs/stitch-screens-mapping.md`).  
   Extrae las URLs de imágenes de cada pantalla y descárgalas aquí, por ejemplo:
   ```bash
   curl -L -o "wwwroot/assets/sentry/nombre-del-asset.png" "URL_DE_LA_IMAGEN"
   ```

2. **Exportación manual desde Stitch**  
   Si exportas HTML/CSS desde la herramienta Stitch, copia las imágenes referenciadas en esta carpeta y actualiza las rutas en los componentes Blazor para que apunten a `/assets/sentry/...`.

## Estructura sugerida

- `wwwroot/assets/sentry/login/` — assets de la pantalla de login.
- `wwwroot/assets/sentry/dashboard/` — assets del dashboard principal.
- `wwwroot/assets/sentry/users/` — assets de gestión de usuarios.
- `wwwroot/assets/sentry/avatar-customizer/` — assets del personalizador de avatar.
- `wwwroot/assets/sentry/avatar-profile/` — assets del perfil de avatar del usuario.
- `wwwroot/assets/sentry/gallery/` — assets de la galería de avatares por sede.

Las rutas en los componentes deben usar `~/assets/sentry/...` o `/assets/sentry/...` para que se resuelvan correctamente en Blazor.
