// Services/VisemeMapper.cs
using System;
using System.Collections.Generic;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Services
{
    public static class VisemeMapper
    {
        private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            // Si tu rig tiene viseme_PP/FF, usa esas líneas comentadas.
            // P/B/M
            // ["p"] = new[]{ "viseme_PP" }, ["b"] = new[]{ "viseme_PP" }, ["m"] = new[]{ "viseme_PP" },
            ["p"] = new[]{ "viseme_aa" },
            ["b"] = new[]{ "viseme_aa" },
            ["m"] = new[]{ "viseme_aa" },

            // F/V
            // ["f"] = new[]{ "viseme_FF" },
            ["f"] = new[]{ "viseme_E" },

            // T/D
            ["t"] = new[]{ "viseme_DD" },
            ["d"] = new[]{ "viseme_DD" },

            // S/Z/SH → Polly usa "S" mayúscula
            ["S"] = new[]{ "viseme_SS" },
            ["s"] = new[]{ "viseme_SS" },
            ["z"] = new[]{ "viseme_SS" },

            // CH/JH
            ["ch"] = new[]{ "viseme_CH" },
            ["jh"] = new[]{ "viseme_CH" },

            // N/L
            ["n"] = new[]{ "viseme_nn" },
            ["l"] = new[]{ "viseme_nn" },

            // K/G
            ["k"] = new[]{ "viseme_kk" },
            ["g"] = new[]{ "viseme_kk" },

            // TH/DH (Polly marca "T")
            ["T"] = new[]{ "viseme_E" },

            // R
            ["r"] = new[]{ "viseme_RR" },

            // Vocales
            ["a"] = new[]{ "viseme_aa" },
            ["e"] = new[]{ "viseme_E"  },
            ["i"] = new[]{ "viseme_I"  },
            ["o"] = new[]{ "viseme_O"  },
            ["u"] = new[]{ "viseme_U"  },
        };

        public static IReadOnlyList<Visema> ToArkit(string viseme, int timeMs)
        {
            if (string.IsNullOrWhiteSpace(viseme)) return Array.Empty<Visema>();
            if (!Map.TryGetValue(viseme, out var shapes) || shapes.Length == 0)
                return Array.Empty<Visema>();

            var list = new List<Visema>(shapes.Length);
            foreach (var s in shapes)
                list.Add(new Visema { ShapeKey = s, Tiempo = timeMs });
            return list;
        }
    }
}
