using Avatar_3D_Sentry.Services.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Services;

public class TtsCleanupService : BackgroundService
{
    private readonly ILogger<TtsCleanupService> _logger;
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _env;

    public TtsCleanupService(
        ILogger<TtsCleanupService> logger,
        IOptions<StorageOptions> options,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _options = options.Value;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.AudioRetentionDays <= 0)
        {
            _logger.LogInformation("TTS cleanup deshabilitado (AudioRetentionDays <= 0).");
            return;
        }

        // Espera inicial para no saturar el arranque
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ContinueWith(_ => { });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error durante la limpieza de audios TTS");
            }

            // Corre una vez al día
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken).ContinueWith(_ => { });
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-_options.AudioRetentionDays);
        _logger.LogInformation("Iniciando limpieza de audios TTS anteriores a {Threshold:O}", threshold);

        if (!string.IsNullOrWhiteSpace(_options.AzureConnection) &&
            !string.Equals(_options.AzureConnection, "env:AZURE_STORAGE_CONNECTION", StringComparison.OrdinalIgnoreCase))
        {
            await CleanupAzureAsync(threshold, ct);
        }
        else
        {
            CleanupLocal(threshold);
        }
    }

    private async Task CleanupAzureAsync(DateTimeOffset threshold, CancellationToken ct)
    {
        var containerName = _options.Containers.Audio;
        var client = new BlobContainerClient(_options.AzureConnection!, containerName);

        await foreach (var blob in client.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: null, cancellationToken: ct))
        {
            var lastMod = blob.Properties.LastModified ?? blob.Properties.CreatedOn;
            if (lastMod is null || lastMod.Value >= threshold) continue;

            try
            {
                await client.DeleteBlobIfExistsAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
                _logger.LogDebug("Eliminado blob de audio antiguo: {Name}", blob.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el blob {Name}", blob.Name);
            }
        }
    }

    private void CleanupLocal(DateTimeOffset threshold)
    {
        var audioPath = _options.Local.AudioPath;
        var root = Path.IsPathRooted(audioPath) ? audioPath : Path.Combine(_env.ContentRootPath, audioPath);

        if (!Directory.Exists(root)) return;

        foreach (var file in Directory.EnumerateFiles(root, "*.mp3", SearchOption.AllDirectories))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc < threshold.UtcDateTime)
                {
                    info.Delete();
                    _logger.LogDebug("Eliminado audio local antiguo: {File}", file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el archivo {File}", file);
            }
        }
    }
}
