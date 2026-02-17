# Contrato API interna `/internal/*`

Documento de referencia para la API interna SaaS (companies, sites, voices, avatar-config, users). **Esta API y la DB no están en este repositorio**: según el documento técnico MetaFusion→Sentry, la API que conecta la DB con el sistema la proporciona el equipo Sentry. Este BFF (AvatarBack) solo **consume** esa API vía `InternalApi:BaseUrl` cuando esté configurada; si `InternalApi:BaseUrl` está vacío, el BFF arranca pero login y gestión users/avatar-config devolverán error claro.

## Base URL y autenticación

- **Base:** `{InternalApi:BaseUrl}/`
- **Auth:** Header `X-Api-Key: {InternalApi:ApiKey}` o Bearer token obtenido con `InternalApi:AuthUser` / `InternalApi:AuthPassword`.

## Endpoints objetivo

### Companies (a implementar)

| Método | Ruta | Descripción | RBAC |
|--------|------|-------------|------|
| GET | `internal/companies` | Listar companies (paginado) | SuperAdmin, CompanyAdmin (scope) |
| GET | `internal/companies/{id}` | Obtener por id | SuperAdmin, CompanyAdmin (propio) |
| POST | `internal/companies` | Crear | SuperAdmin |
| PUT | `internal/companies/{id}` | Actualizar | SuperAdmin |
| DELETE | `internal/companies/{id}` | Desactivar/eliminar | SuperAdmin |

### Sites (a implementar)

| Método | Ruta | Descripción | RBAC |
|--------|------|-------------|------|
| GET | `internal/sites` | Listar por companyId/scope | SuperAdmin, CompanyAdmin, SiteAdmin |
| GET | `internal/sites/{id}` | Obtener por id | SuperAdmin, CompanyAdmin, SiteAdmin (scope) |
| POST | `internal/sites` | Crear | SuperAdmin, CompanyAdmin |
| PUT | `internal/sites/{id}` | Actualizar | SuperAdmin, CompanyAdmin, SiteAdmin (scope) |
| DELETE | `internal/sites/{id}` | Desactivar | SuperAdmin, CompanyAdmin |

### Voices (a implementar)

| Método | Ruta | Descripción | RBAC |
|--------|------|-------------|------|
| GET | `internal/voices` | Catálogo de voces (por idioma/scope) | Todos autenticados internos |
| GET | `internal/voices/{id}` | Detalle voz | Todos autenticados internos |

### Avatar config (parcialmente existente vía BFF → otro servicio)

| Método | Ruta | Descripción | RBAC |
|--------|------|-------------|------|
| GET | `internal/avatar-config` | Listar (query: empresa, sede, page, pageSize) | Internal-only |
| GET | `internal/avatar-config/by-scope` | Por empresa + sede | Internal-only |
| GET | `internal/avatar-config/{id}` | Por id | Internal-only |
| POST | `internal/avatar-config` | Crear | Internal-only |
| PUT | `internal/avatar-config/{id}` | Actualizar | Internal-only |
| DELETE | `internal/avatar-config/{id}` | Eliminar | Internal-only |

**Payload respuesta (con assets):**

- `LogoPath`, `BackgroundPath`: fuente de verdad (persistidos).
- `LogoUrl`, `BackgroundUrl`: derivados on-demand (SAS si Azure).
- `UrlExpiresAtUtc`: ISO 8601 UTC; el cliente puede refrescar antes.

### Users (parcialmente existente)

| Método | Ruta | Descripción | RBAC |
|--------|------|-------------|------|
| GET | `internal/users` | Listar (query: page, pageSize, email, role) | Internal-only |
| GET | `internal/users/by-email/{email}` | Por email (login BFF) | Internal-only, no exponer al cliente |
| GET | `internal/users/{id}` | Por id | Internal-only |
| POST | `internal/users` | Crear | Internal-only |
| PUT | `internal/users/{id}` | Actualizar | Internal-only |
| DELETE | `internal/users/{id}` | Eliminar | Internal-only |

## Matriz RBAC por rol

| Rol | Companies | Sites | Voices | AvatarConfig | Users |
|-----|-----------|-------|--------|--------------|-------|
| SuperAdmin | CRUD | CRUD | R | CRUD | CRUD |
| CompanyAdmin | R (propio) | CRUD (scope) | R | CRUD (scope) | CRUD (scope) |
| SiteAdmin | — | R/PUT (propio) | R | CRUD (scope) | R (scope) |
| AvatarEditor | — | — | R | R/PUT (scope) | — |

## Ejemplos de payload (assets)

**Respuesta 200 – AvatarConfig con URLs:**

```json
{
  "id": 1,
  "empresa": "ACME",
  "sede": "CENTRO",
  "logoPath": "public/acme/centro/logos/logo.png",
  "logoUrl": "https://...blob.core.windows.net/public/...?sv=...&se=...",
  "backgroundPath": "public/acme/centro/fondos/bg.png",
  "backgroundUrl": "https://...",
  "urlExpiresAtUtc": "2026-02-16T15:30:00Z",
  "isActive": true
}
```

**Error 400 – validación:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "detail": "Empresa y Sede son requeridos."
}
```

**Error 403 – scope:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "detail": "No tiene permiso para este recurso."
}
```

## OpenAPI BFF (AvatarBack)

- **Swagger UI:** `https://{host}/swagger` (en desarrollo).
- **JSON:** `GET /swagger/v1/swagger.json` para exportar el contrato actual del BFF.
