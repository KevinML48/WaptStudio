using System;

namespace WaptStudio.Core.Models;

public sealed class CommandExecutionResult
{
    public const string DefaultExecutableName = "wapt-get.exe";

    public string FileName { get; init; } = string.Empty;

    public string Arguments { get; init; } = string.Empty;

    public string ExecutedCommand { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public int ExitCode { get; init; }

    public bool TimedOut { get; init; }

    public bool IsDryRun { get; init; }

    public bool IsSkipped { get; init; }

    public bool IsConfigurationBlocked { get; init; }

    public bool RequiresCredentialPrompt { get; init; }

    public bool RequiresExternalManualWorkflow { get; init; }

    public bool WasInteractiveExecutionAttempted { get; init; }

    public bool ManualFallbackRecommended { get; init; }

    public bool RequiresUserInteraction => RequiresCredentialPrompt || RequiresExternalManualWorkflow;

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    public TimeSpan Duration { get; init; }

    public bool IsSuccess => !TimedOut && !IsConfigurationBlocked && !RequiresUserInteraction && ExitCode == 0;

    public string Summary => IsDryRun
        ? "Succes simule en dry-run."
        : IsConfigurationBlocked
            ? (string.IsNullOrWhiteSpace(StandardError) ? "Action bloquee par la configuration." : StandardError)
            : RequiresCredentialPrompt
                ? (string.IsNullOrWhiteSpace(StandardError) ? "Informations sensibles requises pour continuer." : StandardError)
            : RequiresExternalManualWorkflow
                ? (string.IsNullOrWhiteSpace(StandardError) ? "Interaction utilisateur requise pour terminer la commande." : StandardError)
            : IsSuccess
                ? IsSkipped
                    ? "Commande preparee mais non executee."
                    : $"Commande terminee avec succes en {Duration.TotalSeconds:F1}s."
                : TimedOut
                    ? $"Commande interrompue apres depassement du timeout ({Duration.TotalSeconds:F1}s)."
                    : $"Commande terminee avec code {ExitCode} en {Duration.TotalSeconds:F1}s.";
}
