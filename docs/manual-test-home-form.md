# Prueba manual: envío del selector de configuración

## Contexto
.NET 8 requiere que los formularios que usan `EditForm` incluyan un `FormName` único cuando la página aloja múltiples formularios o componentes que realizan POST. Sin este atributo, la API regresaba el error "The POST request does not specify which form is being submitted" al intentar cargar la configuración inicial.

## Pasos
1. Iniciar la aplicación `AvatarAdmin` en modo desarrollo.
2. Abrir `https://localhost:5001/` y localizar el formulario de selección de empresa y sede.
3. Ingresar valores válidos (ej. `sentry` y `pereira`).
4. Enviar el formulario y confirmar que la respuesta se procesa correctamente.

## Resultado esperado
La solicitud POST debe completarse sin registrar "The POST request does not specify which form is being submitted". El panel carga o crea la configuración según corresponda.

## Nota de implementación
El componente `Home.razor` define ahora `FormName="home-selector-config"` en el `EditForm`. Si se crea otro formulario en la página, debe asignársele un identificador distinto para evitar colisiones.
