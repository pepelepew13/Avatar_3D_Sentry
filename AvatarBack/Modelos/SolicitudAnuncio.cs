using System.ComponentModel.DataAnnotations;

namespace Avatar_3D_Sentry.Modelos
{
    public class SolicitudAnuncio
    {
        [Required]
        public string Empresa { get; set; } = string.Empty;
        
        [Required]
        public string Sede { get; set; } = string.Empty;

        public string Modulo { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        
        // Texto libre opcional si no se usan plantillas
        public string Texto { get; set; } = string.Empty; 
        
        public string Nombre { get; set; } = string.Empty;

        // NUEVO: Campo para soportar el requerimiento multilenguaje
        // Valores esperados: "es", "en", "pt"
        public string Idioma { get; set; } = "es";
    }
}