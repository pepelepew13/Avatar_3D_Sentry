# Opciones de servicios TTS

Este documento resume precios aproximados y soporte de visemas para los principales proveedores de texto a voz.

## AWS Polly
- **Precio**: ~USD 16 por 1 millón de caracteres para voces estándar y ~USD 24 para voces neuronales. Primeros 5 millones de caracteres gratis cada mes durante 12 meses.
- **Visemas**: Sí. Entrega marcas de voz (`speech marks`) con información de visemas que permiten sincronizar movimientos de boca.

## Azure Text to Speech
- **Precio**: ~USD 16 por 1 millón de caracteres para voces estándar y ~USD 24 para voces neuronales.
- **Visemas**: Sí. Proporciona eventos de visema como parte de la síntesis con SSML.

## Google Cloud Text-to-Speech
- **Precio**: ~USD 4 por 1 millón de caracteres para voces estándar y ~USD 16 para voces WaveNet.
- **Visemas**: No ofrece datos de visemas de forma nativa.

## Elección
Aunque Google Cloud TTS ofrece el menor costo por carácter, carece de soporte nativo de visemas. Tanto AWS Polly como Azure TTS proporcionan visemas, pero Polly incluye un nivel gratuito generoso y costos comparables, por lo que se selecciona **AWS Polly** como la opción más rentable que satisface los requisitos del proyecto.