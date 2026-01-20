namespace AvatarSentry.Application.Models;

public class TtsResult
{
    public byte[] AudioBytes { get; set; } = Array.Empty<byte>();
    public int DurationMs { get; set; }
    public List<VisemeDto> Visemes { get; set; } = new();
}
