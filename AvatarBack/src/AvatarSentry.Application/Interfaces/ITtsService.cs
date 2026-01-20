using AvatarSentry.Application.Models;

namespace AvatarSentry.Application.Interfaces;

public interface ITtsService
{
    Task<TtsResult> SynthesizeAsync(string text, string language, string? voiceOverride, CancellationToken cancellationToken);
}
