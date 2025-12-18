// AvatarBack/Modelos/TtsResultado.cs
using System;
using System.Collections.Generic;

namespace Avatar_3D_Sentry.Modelos
{
    public class Visema
    {
        /// <summary>Nombre del blendshape que esperará el visor (p.ej. "viseme_aa").</summary>
        public string ShapeKey { get; set; } = string.Empty;

        /// <summary>Tiempo en milisegundos desde el inicio del audio.</summary>
        public int Tiempo { get; set; }

        /// <summary>Opcional: ID de visema del proveedor (Azure), útil para debug.</summary>
        public int? Id { get; set; }
    }

    public class TtsResultado
    {
        /// <summary>Audio sintetizado (MP3 recomendado por latencia).</summary>
        public byte[] AudioBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Alias para compatibilidad con código que usa tts.Audio.
        /// Internamente usa AudioBytes.
        /// </summary>
        public byte[] Audio
        {
            get => AudioBytes;
            set => AudioBytes = value ?? Array.Empty<byte>();
        }

        /// <summary>Duración del audio en milisegundos.</summary>
        public int DurationMs { get; set; }

        /// <summary>Visemas mapeados a shape keys que entiende el visor.</summary>
        public List<Visema> Visemas { get; set; } = new();

        /// <summary>
        /// Alias en inglés para compatibilidad con código que usa "Visemes".
        /// Apunta a la misma lista que <see cref="Visemas"/>.
        /// </summary>
        public List<Visema> Visemes
        {
            get => Visemas;
            set => Visemas = value ?? new List<Visema>();
        }
    }
}
