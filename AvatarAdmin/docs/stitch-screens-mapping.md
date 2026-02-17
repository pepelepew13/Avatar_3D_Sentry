# Mapeo pantallas Stitch → componentes Blazor (Sentry Admin Dashboard)

Proyecto Stitch: **Sentry Admin Dashboard** (ID: `901339848894182002`).

## Limitación actual

El agente no tiene acceso al MCP de Stitch (`get_project`, `get_screen`). Este documento sirve para:

1. Saber qué pantalla de Stitch corresponde a qué página/componente Blazor.
2. Cuando tengas el HTML/CSS e imágenes (vía MCP u exportación manual), integrarlos en los componentes indicados.

---

## Lista de 19 pantallas y mapeo

| # | Screen ID (Stitch) | Nombre pantalla | Componente / ruta Blazor | Notas |
|---|--------------------|------------------|---------------------------|--------|
| 1 | `0c163ce7e5c5434d88f4fd4403ea482b` | Sentry User Avatar Profile | `Pages/UserAvatarProfile.razor` (nuevo) o reutilizar Editor | Perfil de avatar del usuario |
| 2 | `0e448fe8ef9c4a67ae00b018d4833b52` | Sentry Avatar Customizer | `Pages/Editor.razor` | Editor / personalizador |
| 3 | `158fc875b0004c3ab10849ae35c9dd98` | Sentry Platform Login | `Pages/Auth.razor` | Login |
| 4 | `197ea1f4194b4ecc82db9297bd6f63be` | Sentry User Avatar Profile | Igual que #1 | Variante de perfil |
| 5 | `2f74dbdd57ed48b0b7c172d18412f15b` | Sentry User Management | `Pages/Users.razor` | Gestión de usuarios |
| 6 | `33a1241c27834a288dc1d6878cd87b45` | Sentry Avatar Customizer | `Pages/Editor.razor` | Variante customizer |
| 7 | `42b99985c4b74ebd94a873e2c368d1da` | Galería de Avatares por Sede | `Pages/AvatarGallery.razor` (nuevo) | Galería por sede |
| 8 | `578f5cdbbdc44cef88e27cae98ea5bcc` | Sentry User Avatar Profile | Igual que #1 | Variante perfil |
| 9 | `62aea1ead0a049e89222f5f4a94351fc` | Sentry Avatar Customizer | `Pages/Editor.razor` | Variante customizer |
| 10 | `72dbd4c5a13d49f3a0c7b86bd73556e0` | Sentry User Management | `Pages/Users.razor` | Variante user management |
| 11 | `95e47c91716549a7933eafd8b3413552` | Sentry User Management | `Pages/Users.razor` | Variante user management |
| 12 | `9ed8367d1b644e7ba7dc828f095dc82e` | Sentry Avatar Customizer | `Pages/Editor.razor` | Variante customizer |
| 13 | `baba0aea2d1e413b9595c3c942fc20b8` | Sentry Admin Dashboard | `Pages/Home.razor` | Dashboard principal |
| 14 | `bae02f35276a4429ae631fbbbbc9e53a` | Sentry User Avatar Profile | Igual que #1 | Variante perfil |
| 15 | `c2445135ae8449268d69218901fac12f` | Sentry User Avatar Profile | Igual que #1 | Variante perfil |
| 16 | `d4a409d6c7634b3c83ab6820803a48b4` | Sentry User Avatar Profile | Igual que #1 | Variante perfil |
| 17 | `e7f5e245dca74a979f8f8ff57c08bb19` | Sentry User Management | `Pages/Users.razor` | Variante user management |
| 18 | `f58c0b1ba47042ad8d9ff542cb1cdcaa` | Sentry Avatar Customizer | `Pages/Editor.razor` | Variante customizer |
| 19 | `faa7786d298b43a8a9131d1c2ed820d9` | Sentry User Avatar Profile | Igual que #1 | Variante perfil |

---

## Resumen por tipo

| Tipo | Pantallas | Componente Blazor |
|------|-----------|-------------------|
| **Login** | 1 | `Auth.razor` @ `/auth` |
| **Admin Dashboard** | 1 | `Home.razor` @ `/` |
| **User Management** | 4 | `Users.razor` @ `/users` |
| **Avatar Customizer** | 5 | `Editor.razor` @ `/editor` |
| **User Avatar Profile** | 7 | `UserAvatarProfile.razor` (crear) o `Editor.razor` |
| **Galería por Sede** | 1 | `AvatarGallery.razor` (crear) @ `/gallery` |

---

## Pasos para integrar cuando tengas datos de Stitch

1. **Por cada pantalla:** usar `get_screen` (o exportar) y obtener:
   - HTML (o descripción de estructura).
   - CSS (o clases/tokens).
   - URLs de todas las imágenes/iconos.

2. **Descargar assets:**
   ```bash
   curl -L -o "AvatarAdmin/wwwroot/assets/sentry/<categoria>/<nombre>.png" "<URL>"
   ```
   Categorías sugeridas: `login`, `dashboard`, `users`, `avatar-customizer`, `avatar-profile`, `gallery`.

3. **Implementar o actualizar componentes:**
   - **Auth.razor:** reemplazar o ajustar markup/CSS con el diseño de la pantalla Login.
   - **Home.razor:** con el diseño del Admin Dashboard.
   - **Users.razor:** con las variantes de User Management (elegir la que mejor encaje o combinar).
   - **Editor.razor:** con las variantes de Avatar Customizer.
   - Crear **UserAvatarProfile.razor** si el perfil de avatar es distinto del editor.
   - Crear **AvatarGallery.razor** para la galería por sede.

4. **Rutas de imágenes en Blazor:** usar siempre `/assets/sentry/...` o `~/assets/sentry/...`.

Cuando configures el MCP de Stitch en Cursor, podrás pedir de nuevo: "obtén el código e imágenes de las 19 pantallas y genera los componentes" y el agente usará `get_project` + `get_screen` y esta guía para generar el frontend.

---

## Assets descargados (19 pantallas)

Todos los HTML y screenshots se guardaron con la convención:

- HTML: `{categoria}/screen-{screenId}.html`
- Screenshot: `{categoria}/screenshot-{screenId}.png`

Categorías: `login`, `dashboard`, `users`, `avatar-customizer`, `avatar-profile`, `gallery`.

| Componente | Pantalla de referencia | Archivos |
|------------|------------------------|----------|
| Auth.razor | Login (158fc875…) | login/screen-*.html, login/avatar-bg.png (ya existía) |
| Home.razor | Dashboard (baba0aea…) | dashboard/screen-baba0aea….html, screenshot |
| Users.razor | User Management (2f74dbdd…) | users/screen-2f74dbdd….html, screenshot |
| Editor.razor | Avatar Customizer (0e448fe8…) | avatar-customizer/screen-0e448fe8….html, screenshot |
| UserAvatarProfile.razor | User Avatar Profile (0c163ce7…) | avatar-profile/screen-0c163ce7….html, screenshot |
| AvatarGallery.razor | Galería (42b99985…) | gallery/screen-42b99985….html, screenshot |

Script de descarga: `wwwroot/assets/sentry/downloads.ps1`.
