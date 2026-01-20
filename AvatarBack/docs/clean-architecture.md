# Clean Architecture layout (AvatarBack)

This project is being reorganized to follow a **Clean Architecture**-style layout with
clear layers and module-based folders.

## Layer map

```
AvatarBack/
  Api/
    Controllers/
      Assets/
      Auth/
      Avatar/
      AvatarConfigs/
      AvatarEditor/
      Models/
      Tts/
      Users/
    Middleware/
    Security/
    Swagger/
  Application/
    Contracts/
    AvatarConfigs/
    Config/
    InternalApi/
    Users/
  Infrastructure/
    Data/
    Services/
      Storage/
    Settings/
```

### Responsibilities

- **Api**: HTTP layer (controllers, filters, middleware, auth/authorization helpers).
- **Application**: DTOs, internal API clients, and use-case oriented types.
- **Infrastructure**: external integrations (storage, TTS, data contexts, options).

## Module-based organization

Controllers are grouped by module to match the API surface:

- `Assets` → file uploads / asset management
- `Auth` → authentication endpoints
- `Avatar` → public avatar config + announce
- `AvatarConfigs` → admin config CRUD
- `AvatarEditor` → editor-specific endpoints
- `Models` → model file access
- `Tts` → standalone TTS endpoints
- `Users` → user management endpoints

## Next steps

- Migrate remaining namespaces to match the new folder layout.
- Consolidate endpoints to match the spec in `docs/` and remove deprecated routes.
- Extract use cases into Application services and keep controllers thin.
