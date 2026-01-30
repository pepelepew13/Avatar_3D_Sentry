using System;
using System.Text.Json.Serialization;

namespace Avatar_3D_Sentry.Modelos;

public class AvatarConfig
{
    private string _empresa = string.Empty;
    private string _sede = string.Empty;

    public int Id { get; set; }

    public bool IsActive { get; set; } = true;

    public required string Empresa
    {
        get => _empresa;
        set { ArgumentNullException.ThrowIfNull(value); _empresa = value; UpdateNormalizedEmpresa(); }
    }

    [JsonIgnore] public string NormalizedEmpresa { get; private set; } = string.Empty;

    public required string Sede
    {
        get => _sede;
        set { ArgumentNullException.ThrowIfNull(value); _sede = value; UpdateNormalizedSede(); }
    }

    [JsonIgnore] public string NormalizedSede { get; private set; } = string.Empty;

    public void Normalize() { UpdateNormalizedEmpresa(); UpdateNormalizedSede(); }

    // === Apariencia
    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }              // ahora guardaremos "/assets/{id}" o URL externa
    public string? ProveedorTts { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? ColorCabello { get; set; }
    public string? BackgroundPath { get; set; }

    // === Identidad
    public string? LogoPath { get; set; }           // igual: guardamos "/assets/{id}"

    private void UpdateNormalizedEmpresa() => NormalizedEmpresa = _empresa.Trim().ToLowerInvariant();
    private void UpdateNormalizedSede()    => NormalizedSede    = _sede.Trim().ToLowerInvariant();
}
