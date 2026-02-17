# Definition of Done (DoD) por módulo

Criterios para considerar cerrado cada módulo del plan SaaS.

## API interna (`/internal/*`)

- [ ] Endpoints companies, sites, voices, avatar-config, users (y `users/by-email`) implementados y documentados.
- [ ] RBAC y validación de scope aplicados en todas las operaciones mutables y lecturas sensibles.
- [ ] Respuestas de avatar-config incluyen `Path`, `Url` derivada y `UrlExpiresAtUtc`.
- [ ] Contrato OpenAPI (o equivalente) publicado y estable.
- [ ] Pruebas de contrato/integración para endpoints críticos.

## BFF (AvatarBack)

- [ ] Login sin comparación en texto plano: verificación de PasswordHash solo en backend.
- [ ] JWT con claims de rol y scope (empresa/sede o companyId/siteId).
- [ ] Integración con API interna con token cacheado y manejo de 401 (refresh/retry).
- [ ] Respuestas de config con `UrlExpiresAtUtc` y URLs SAS on-demand.
- [ ] Políticas de autorización (CanEditAvatar, etc.) alineadas con los 4 roles.

## UI Admin (AvatarAdmin)

- [ ] Guards por rol: SuperAdmin, CompanyAdmin, SiteAdmin, AvatarEditor; sin operaciones fuera de scope.
- [ ] Listados y formularios adaptados a scope Company/Site (o Empresa/Sede con flag legacy).
- [ ] Formularios envían/reciben Path; cliente usa UrlExpiresAtUtc para refresh silencioso de SAS.
- [ ] Flujos de estado de AvatarConfig (Draft/PendingApproval/Active/Disabled) si aplican.
- [ ] Módulos Companies, Sites, Users, AvatarConfig, Voices, KPIs operativos.

## Datos y migración

- [ ] Migraciones ejecutadas en dev/qa/prod con integridad referencial validada.
- [ ] Backfill Company/Site desde datos legacy sin pérdida de historial.
- [ ] Feature flag y plan de retiro para compatibilidad empresa/sede.
- [ ] Rollback seguro por versión de esquema documentado.

## Hardening y operación

- [ ] Logs estructurados y trazas por requestId.
- [ ] Métricas (latencia TTS, SAS, errores auth) y alertas/tableros.
- [ ] Rate limiting y validación de entrada en endpoints públicos e internos.
- [ ] Runbooks y plan de despliegue progresivo con rollback y checklist de go-live.
