# Visemas (lip-sync), mallas y materiales del avatar

Referencia técnica para sincronización labial (Azure TTS → Blender Shape Keys), cabello, logo y fondo.

## 1. Mapeo de visemas: Azure Viseme ID → Shape Key (Blender)

Cuando Azure Speech devuelve eventos de visema, el backend mapea el **Azure Viseme ID** al **Shape Key** que tiene el modelo 3D en Blender. El visor aplica ese shape key en el tiempo indicado (milisegundos desde el inicio del audio).

| Shape Key (Blender) | Descripción        | Azure Viseme ID | Fonemas (Azure)   |
|---------------------|--------------------|------------------|-------------------|
| viseme_sil          | Silencio/boca neutra | 0              | silence           |
| viseme_PP           | Labiales cerrados  | 21               | p, b, m           |
| viseme_FF           | Labio-diente       | 18               | f, v              |
| viseme_TH           | Lengua entre dientes | 17             | th, dh            |
| viseme_DD           | D/T/N              | 19               | t, d, n           |
| viseme_kk           | K/G/NG             | 20               | k, g, ng          |
| viseme_CH           | CH/SH/JH           | 16               | sh, ch, jh, zh    |
| viseme_SS           | S/Z                | 15               | s, z              |
| viseme_nn           | N/L (alveolar)     | 14               | l                 |
| viseme_RR           | R                  | 13               | r                 |
| viseme_aa           | A abierta          | 2                | a                 |
| viseme_EE           | E media            | 4                | e                 |
| viseme_II           | I cerrada          | 6                | i                 |
| viseme_OO           | O redondeada       | 8                | o, ow             |
| viseme_UU           | U redondeada       | 7                | u, w, uw          |

Implementación: `AzureTtsService.MapAzureVisemeToShapeKey` (backend) y normalización de nombres en el visor (avatarViewer.*.js).

---

## 2. Nomenclatura de mallas y materiales 3D

El modelo .glb debe exponer estas mallas y materiales para que el visor pueda personalizar **cabello** y **logo**.

### Cabello (color de cabello)

| Tipo     | Nombre (documento)        | Uso |
|----------|---------------------------|-----|
| Malla    | `avaturn_hair_0` / `avaturn_hair_1` | El visor busca estas mallas para aplicar el color de cabello (hex). |
| Material | `avaturn_hair_0_material` / `avaturn_hair_1_material` | Materiales a los que se aplica el tinte. |

El administrador elige un color (ej. `ColorCabello` en la config); el frontend lo convierte a hex y el visor lo aplica a las mallas/materiales de cabello.

### Logo (etiqueta corporativa)

| Tipo     | Nombre (documento) | Uso |
|----------|--------------------|-----|
| Malla    | `LogoMesh`         | Malla que lleva la textura del logo. |
| Material | `LogoLabel`        | Material cuya textura (map) se reemplaza por la imagen PNG/JPG subida por el administrador. |

El visor busca la malla `LogoMesh` (o materiales con nombre `LogoLabel`) y asigna al material la URL de la imagen del logo. Si no existe la malla, se puede crear un plano de respaldo (comportamiento actual).

---

## 3. Fondo detrás del avatar (imagen)

El **fondo** de la escena puede ser:

- **Color plano**: valor hex o nombre de preset (ej. `oficina`, `moderno`).
- **Imagen**: URL de una imagen (PNG, JPG). El visor carga la textura y la asigna a `scene.background`. Si la imagen es equirectangular (360°), se usa también como environment para reflexiones PBR; si no, se usa como fondo plano o backdrop.

La config del avatar guarda `BackgroundPath` (ruta persistida del asset); la API devuelve `BackgroundUrl` (SAS o URL pública) para que el cliente la use como fondo. Así el fondo detrás del avatar **puede cambiarse mediante una imagen** subida por la empresa.

---

## Resumen de flujo

1. **TTS**: Backend genera audio + lista de visemas (Azure Viseme ID + tiempo). Se mapea ID → Shape Key y se envía al cliente; el visor aplica los morphs en sincronía con el audio.
2. **Cabello**: Cliente envía `hairColor` (hex); el visor busca `avaturn_hair_0`, `avaturn_hair_1` y sus materiales y aplica el color.
3. **Logo**: Cliente envía `logoUrl`; el visor busca `LogoMesh`/material `LogoLabel` y asigna la textura.
4. **Fondo**: Cliente envía `background` (preset o URL de imagen); el visor asigna color o carga la imagen como fondo de escena (y opcionalmente environment).
