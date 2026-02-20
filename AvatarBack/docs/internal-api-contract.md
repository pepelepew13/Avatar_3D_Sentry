# Contrato API interna `/internal/*`

Documento de referencia para la API interna SaaS (companies, sites, voices, avatar-config, users, kpis). **Esta API y la DB no están en este repositorio**: según el documento técnico MetaFusion→Sentry, la API que conecta la DB con el sistema la proporciona el equipo Sentry. Este BFF (AvatarBack) solo **consume** esa API vía `InternalApi:BaseUrl` cuando esté configurada; si `InternalApi:BaseUrl` está vacío, el BFF arranca pero login y gestión users/avatar-config devolverán error claro.

## Base URL y autenticación

- **Base:** `{InternalApi:BaseUrl}/`  
  Debe incluir la ruta completa hasta la raíz de la API. Ejemplo:  
  `https://desarrollo_03.sistemasentry.com.co/AndreaGonzalez/UserAvatarApi/`  
  (el BFF luego concatena `api/Token/Authentication` o `internal/...`).

- **Autenticación:** Todos los endpoints internos requieren **token Bearer**. El token se obtiene con `POST api/Token/Authentication`; sin token válido o con credenciales incorrectas la API devuelve 401 y el BFF no puede consumir los endpoints.

### Obtención del token (POST api/Token/Authentication)

- **Body (JSON):** Propiedades en **PascalCase**: `User` y `Password` (string, string).
- **Valores:** Según documentación UserAvatarApi, *"Los valores de User y Password deben ser enviados con encriptación del comconfig"*. Es decir, en configuración del BFF (`InternalApi:AuthUser` e `InternalApi:AuthPassword`) deben ir exactamente los mismos valores que proporciona el equipo de la API (por ejemplo los valores encriptados del comconfig), no usuario/contraseña en claro.
- **Respuesta 200:** `{ "token": "eyJhbGciOiJIUzI1NiIs..." }`. Ese valor se envía en el header: `Authorization: Bearer {token}`.
- **Duración:** Por defecto el token tiene una duración de 90 días (según doc); el BFF puede cachearlo y renovarlo ante 401.

Ejemplo (curl) según documentación:

```bash
curl -X 'POST' \
  'https://desarrollo_03.sistemasentry.com.co/AndreaGonzalez/UserAvatarApi/api/Token/Authentication' \
  -H 'Content-Type: application/json' \
  -d '{ "User": "9gk7sPAj9hE=", "Password": "NsX8xEav35+BvurRn3x2bANt7lnq2RJ6odp/zr3HQ+k=" }'
```

Si el BFF no obtiene el token, suele deberse a:

1. **BaseUrl incorrecta:** debe ser la base completa (incluyendo path tipo `.../UserAvatarApi/`) sin el segmento `api/Token/Authentication`.
2. **Credenciales incorrectas:** `AuthUser` y `AuthPassword` deben ser los valores con encriptación del comconfig proporcionados por el equipo de la API; no usar usuario/contraseña en texto plano si la API espera valores encriptados.

## Endpoints (contrato real desplegado)

### AvatarConfig

| Método | Ruta | Query / Body | Respuesta |
|--------|------|--------------|-----------|
| GET | `internal/avatar-config/by-scope` | `company` (int), `site` (int) | 200: objeto AvatarConfig (id, companyId, siteId, modelUrl, backgroundUrl, logoUrl, urlExpiresAtUtc, language, hairColor, voiceIds[], status, isActive, createdAtUtc, updatedAtUtc) |
| GET | `internal/avatar-config` | `company`, `site`, `page`, `pageSize` (int) | 200: { page, pageSize, total, totalPages, items[] } |
| POST | `internal/avatar-config` | Body: CompanyId, SiteId, ModelPath, BackgroundPath, LogoPath, Language, HairColor, VoiceIds[], Status, IsActive | 201: { success, message, data } |
| PUT | `internal/avatar-config/{id}` | Body: companyId, siteId, modelPath, backgroundPath, logoPath, language, hairColor, voiceIds[], status, isActive | 200: { success, message, data } |
| PATCH | `internal/avatar-config/{id}` | Body: JSON patch | 200 |
| DELETE | `internal/avatar-config/{id}` | — | 200: { success, message, data } |

**Nota:** La API usa **company** y **site** como **IDs enteros**, no como códigos string. El BFF resuelve códigos empresa/sede (p. ej. "ACME", "CENTRO") a companyId/siteId mediante `GET /internal/companies?code=...` y `GET /internal/sites?companyId=...&code=...`.

### Companies

| Método | Ruta | Query / Body | Respuesta |
|--------|------|--------------|-----------|
| GET | `internal/companies` | `code`, `name`, `sector`, `isActive`, `page`, `pageSize` | 200: { page, pageSize, total, items[] } (Id, Name, Code, CorporateId, Sector, LogoPath, LogoUrl, UrlExpiresAtUtc, AssetsRootPath, IsActive, CreatedAtUtc, UpdatedAtUtc) |
| GET | `internal/companies/{id}` | — | 200: objeto Company |
| POST | `internal/companies` | Body: Name, Code, CorporateId, Sector, LogoPath, AssetsRootPath, IsActive | 201 |
| PUT | `internal/companies/{id}` | Body: Name, Code, ... | 200 |
| PATCH | `internal/companies/{id}` | Body: {} | 200 |
| DELETE | `internal/companies/{id}` | — | 200 |

### Sites

| Método | Ruta | Query / Body | Respuesta |
|--------|------|--------------|-----------|
| GET | `internal/sites` | `companyId`, `code`, `name`, `isActive`, `page`, `pageSize` | 200: { page, pageSize, total, items[] } |
| GET | `internal/sites/{id}` | — | 200: objeto Site |
| POST | `internal/sites` | Body: CompanyId, Name, Code, Address, City, Country, IsActive | 200 |
| PUT | `internal/sites/{id}` | Body: CompanyId, Name, Code, ... | 200 |
| PATCH | `internal/sites/{id}` | Body: {} | 200 |
| DELETE | `internal/sites/{id}` | — | 200 |

### Users

| Método | Ruta | Query / Body | Respuesta |
|--------|------|--------------|-----------|
| GET | `internal/users` | `company`, `site` (int), `role`, `email`, `page`, `pageSize` | 200: { page, pageSize, total, items[] } (id, email, fullName, role, companyId, siteId, companyName, siteName, isActive, lastLoginAtUtc, createdAtUtc, updatedAtUtc) |
| GET | `internal/users/{id}` | — | 200: objeto User (sin PasswordHash en listado) |
| GET | `internal/users/by-email/{email}` | — | 200: Id, Email, PasswordHash, FullName, Role, CompanyId, SiteId, IsActive (para auth) |
| POST | `internal/users` | Body: Email, Password, Role, FullName, CompanyId, SiteId, IsActive | 201 |
| PUT | `internal/users/{id}` | Body: Email, Password?, Role, FullName, CompanyId, SiteId, IsActive | 200 |
| DELETE | `internal/users/{id}` | — | 200 |
| POST | `internal/users/{id}/reset-password` | Body: { NewPassword } | 200 |

### Voices

| Método | Ruta | Query | Respuesta |
|--------|------|-------|-----------|
| GET | `internal/voices` | companyId, provider, locale, gender, isActive, page, pageSize | 200: { page, pageSize, total, items[] } (Id, CompanyId, Provider, AzureShortName, DisplayName, Locale, Gender, IsActive) |
| GET | `internal/voices/{id}` | — | 200 |
| POST | `internal/voices` | Body: Provider, AzureShortName, DisplayName, Locale, Gender, CompanyId, IsActive | 201 |
| PUT / PATCH / DELETE | `internal/voices/{id}` | — | 200 |

### KPIs

| Método | Ruta | Respuesta |
|--------|------|-----------|
| GET | `internal/kpis/global` | 200: string |
| GET | `internal/kpis/company/{companyId}` | 200: string |
| GET | `internal/kpis/site/{siteId}` | 200: string |

### Token (auth API interna)

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| POST | `api/Token/Authentication` | `{ "User": "string", "Password": "string" }` (PascalCase; valores según comconfig) | 200: `{ "token": "eyJ..." }` → usar como `Authorization: Bearer {token}` |

## Uso en el BFF

- **Resolución empresa/sede → IDs:** El BFF expone empresa/sede como **códigos** (string) en su API pública. Para llamar a la API interna usa `ICompanySiteResolutionService`: `ResolveToIdsAsync(empresaCode, sedeCode)` llama a `GET /internal/companies?code=...` y `GET /internal/sites?companyId=...&code=...` y devuelve `(CompanyId, SiteId)`.
- **Avatar config:** Los controladores y el data store resuelven empresa/sede a IDs y llaman a `GET /internal/avatar-config/by-scope?company={id}&site={id}` y a list/create/update/delete con los DTOs alineados al contrato (modelUrl/modelPath, backgroundUrl/backgroundPath, logoUrl/logoPath, language, hairColor, voiceIds[], status).
- **Users:** Misma resolución para filtros y para create/update (CompanyId, SiteId). La respuesta de list/get incluye companyName y siteName que el BFF mapea a Empresa/Sede en su DTO público.

## OpenAPI BFF (AvatarBack)

- **Swagger UI:** `https://{host}/swagger` (en desarrollo).
- **JSON:** `GET /swagger/v1/swagger.json` para exportar el contrato actual del BFF.
