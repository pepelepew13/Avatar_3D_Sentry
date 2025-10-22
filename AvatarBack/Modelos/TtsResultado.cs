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

        /// <summary>Opcional: ID de visema de Azure (si sirve para debug/mapeo).</summary>
        public int? Id { get; set; }
    }

    public class TtsResultado
    {
        /// <summary>Audio sintetizado (MP3 recomendado por latencia).</summary>
        public byte[] AudioBytes { get; set; } = Array.Empty<byte>();

        /// <summary>Duración del audio en milisegundos.</summary>
        public int DurationMs { get; set; }

        /// <summary>Visemas mapeados a shape keys que entiende el visor.</summary>
        public List<Visema> Visemes { get; set; } = new();
    }
}
