using System;

namespace WaptStudio.Core.Models;

public sealed class CommandExecutionResult
{
    public string FileName { get; init; } = string.Empty;

    public string Arguments { get; init; } = string.Empty;

    public string ExecutedCommand { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public int ExitCode { get; init; }

    public bool TimedOut { get; init; }

    public bool IsDryRun { get; init; }

    public bool IsSkipped { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    public TimeSpan Duration { get; init; }

    public bool IsSuccess => (IsDryRun || IsSkipped || !TimedOut) && ExitCode == 0;

    public string Summary => IsSuccess
        ? IsDryRun
            ? "Commande simulee en dry-run."
            : IsSkipped
                ? "Commande verifiee mais non executee."
                : $"Commande terminee avec succes en {Duration.TotalSeconds:F1}s."
        : TimedOut
            ? $"Commande interrompue apres depassement du timeout ({Duration.TotalSeconds:F1}s)."
            : $"Commande terminee avec code {ExitCode} en {Duration.TotalSeconds:F1}s.";
}
