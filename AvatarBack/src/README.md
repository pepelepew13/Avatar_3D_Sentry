# Avatar Sentry Clean Architecture (scaffold)

Estructura inicial para migrar el backend a Clean Architecture:

```
src/
  AvatarSentry.Api/            -> ASP.NET Core API (controllers, DI, Swagger)
  AvatarSentry.Application/    -> contratos, DTOs, interfaces y settings
  AvatarSentry.Domain/         -> entidades y reglas de negocio puras
  AvatarSentry.Infrastructure/ -> integraciones externas (UserAvatarApi, Azure, etc.)
```

Este scaffold contiene stubs de endpoints y settings para avanzar en la implementación.
