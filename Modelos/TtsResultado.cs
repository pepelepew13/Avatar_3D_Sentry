using System;
using System.Collections.Generic;

namespace Avatar_3D_Sentry.Modelos;

public class Visema
{
    public string ShapeKey { get; set; } = string.Empty;
    public int Tiempo { get; set; }
}

public class TtsResultado
{
    public byte[] Audio { get; set; } = Array.Empty<byte>();
    public List<Visema> Visemas { get; set; } = new();
}
