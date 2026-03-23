using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class LogService : ILogService
{
    private readonly ISettingsService _settingsService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LogService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public Task LogInfoAsync(string message, CancellationToken cancellationToken = default)
        => WriteAsync("INFO", message, cancellationToken);

    public Task LogWarningAsync(string message, CancellationToken cancellationToken = default)
        => WriteAsync("WARN", message, cancellationToken);

    public Task LogErrorAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var fullMessage = exception is null
            ? message
            : $"{message}{Environment.NewLine}{exception}";

        return WriteAsync("ERROR", fullMessage, cancellationToken);
    }

    private async Task WriteAsync(string level, string message, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        AppPaths.EnsureCreated(settings);
        var logFile = Path.Combine(AppPaths.ResolveLogsDirectory(settings), $"waptstudio-{DateTime.Now:yyyyMMdd}.log");
        var line = $"[{DateTimeOffset.Now:O}] [{level}] {message}{Environment.NewLine}";

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(logFile, line, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
