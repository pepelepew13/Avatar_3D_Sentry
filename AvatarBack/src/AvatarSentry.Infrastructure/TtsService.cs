using AvatarSentry.Application.Interfaces;
using AvatarSentry.Application.Models;

namespace AvatarSentry.Infrastructure;

public class TtsService : ITtsService
{
    public Task<TtsResult> SynthesizeAsync(string text, string language, string? voiceOverride, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TtsResult());
    }
}
