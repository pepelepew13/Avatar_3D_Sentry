# Shape Keys del Avatar

Este documento describe las *shape keys* creadas en Blender para animar los fonemas y expresiones básicas del avatar.

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
