namespace Avatar_3D_Sentry.Modelos;

public class AssetFile
{
    public int Id { get; set; }

    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;

    /// <summary>logo | fondo | audio | modelo</summary>
    public string Tipo { get; set; } = "logo";

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
