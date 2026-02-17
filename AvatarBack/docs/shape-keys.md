# Shape Keys del Avatar

Este documento describe las *shape keys* creadas en Blender para animar los fonemas y expresiones básicas del avatar.

Para el **lip-sync con Azure TTS** se usan los visemas con nombres exactos (`viseme_sil`, `viseme_PP`, `viseme_aa`, etc.). El mapeo Azure Viseme ID → Shape Key y la nomenclatura de mallas/materiales (cabello, logo) están en [visemas-y-mallas.md](visemas-y-mallas.md).

## Visemas (lip-sync)

| Shape Key (Blender) | Uso |
|--------------------|-----|
| viseme_sil | Silencio / boca neutra |
| viseme_PP | p, b, m |
| viseme_FF | f, v |
| viseme_TH | th, dh |
| viseme_DD | t, d, n |
| viseme_kk | k, g, ng |
| viseme_CH | sh, ch, jh, zh |
| viseme_SS | s, z |
| viseme_nn | N/L alveolar |
| viseme_RR | r |
| viseme_aa | A abierta |
| viseme_EE | E media |
| viseme_II | I cerrada |
| viseme_OO | O redondeada |
| viseme_UU | U redondeada |

## Otras expresiones (referencia)

| Shape Key | Finalidad |
|-----------|-----------|
| A | Formar la vocal abierta **A**. |
| E | Configurar la boca para la vocal **E**. |
| I | Posición estrecha para la vocal **I**. |
| O | Redondear los labios para la vocal **O**. |
| U | Redondez pronunciada para la vocal **U**. |
| M | Cerrar los labios para sonidos nasales como **M**. |
| F | Morder suavemente el labio inferior para sonidos **F** o **V**. |
| L | Elevar la lengua para el fonema **L**. |
| B | Preparar los labios cerrados para el fonema **B**. |
| P | Posición similar a **B**, con expulsión de aire para **P**. |
| Smile | Curvar los extremos de la boca hacia arriba. |
| Frown | Curvar los extremos de la boca hacia abajo. |
| Blink_Left | Cerrar el párpado izquierdo. |
| Blink_Right | Cerrar el párpado derecho. |
| Surprise | Abrir la boca y elevar cejas en gesto de sorpresa. |
| Angry | Fruncir el ceño y tensar la boca en gesto de enojo. |

## Uso

Se recomienda utilizar **Blender 3.6 LTS** para generar las *shape keys*.
Desde la raíz del repositorio, ejecuta:

```bash
blender --background --python scripts/generate_shape_keys.py
```

El archivo generado se guardará en `wwwroot/models/avatar_shape_keys.glb`.

> **Nota:** Los archivos `.glb` se excluyen del repositorio para evitar incrementar su tamaño. Tras generar el modelo, guárdalo en la ruta indicada de tu copia local antes de ejecutar el visor del avatar.
