using System;
using System.Text.Json.Serialization;

namespace Avatar_3D_Sentry.Modelos;

/// <summary>
/// Representa la configuración visual de un avatar asociada a una empresa y sede.
/// </summary>
public class AvatarConfig
{
    private string _empresa = string.Empty;
    private string _sede = string.Empty;

    public int Id { get; set; }

    public required string Empresa
    {
        get => _empresa;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _empresa = value;
            UpdateNormalizedEmpresa();
        }
    }

    [JsonIgnore]
    public string NormalizedEmpresa { get; private set; } = string.Empty;

    public required string Sede
    {
        get => _sede;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _sede = value;
            UpdateNormalizedSede();
        }
    }

    [JsonIgnore]
    public string NormalizedSede { get; private set; } = string.Empty;

    public void Normalize()
    {
        UpdateNormalizedEmpresa();
        UpdateNormalizedSede();
    }

    /// <summary>
    /// Ruta del logo aplicado al pecho del modelo.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Información sobre la vestimenta seleccionada.
    /// </summary>
    public string? Vestimenta { get; set; }

    /// <summary>
    /// Configuración del fondo para el avatar.
    /// </summary>
    public string? Fondo { get; set; }

    /// <summary>
    /// Proveedor del servicio TTS seleccionado.
    /// </summary>
    public string? ProveedorTts { get; set; }

    /// <summary>
    /// Voz empleada al generar el audio del avatar.
    /// </summary>
    public string? Voz { get; set; }

    /// <summary>
    /// Idioma preferido para la narración del avatar.
    /// </summary>
    public string? Idioma { get; set; }

    /// <summary>
    /// Color de cabello seleccionado para el avatar.
    /// </summary>
    public string? ColorCabello { get; set; }

    private void UpdateNormalizedEmpresa()
    {
        NormalizedEmpresa = _empresa.Trim().ToLowerInvariant();
    }

    private void UpdateNormalizedSede()
    {
        NormalizedSede = _sede.Trim().ToLowerInvariant();
    }
}

