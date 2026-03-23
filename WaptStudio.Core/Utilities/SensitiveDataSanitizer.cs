using System;
using System.Collections.Generic;
using System.Linq;
using WaptStudio.Core.Models;

namespace WaptStudio.Core.Utilities;

public static class SensitiveDataSanitizer
{
    public static string SanitizeText(string? input, IEnumerable<string>? sensitiveValues)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? string.Empty;
        }

        var output = input;
        foreach (var sensitiveValue in sensitiveValues?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal) ?? [])
        {
            output = output.Replace(sensitiveValue, "[REDACTED]", StringComparison.Ordinal);
        }

        return output;
    }

    public static CommandExecutionResult SanitizeCommandResult(CommandExecutionResult result, IEnumerable<string>? sensitiveValues)
        => new()
        {
            FileName = SanitizeText(result.FileName, sensitiveValues),
            Arguments = SanitizeText(result.Arguments, sensitiveValues),
            ExecutedCommand = SanitizeText(result.ExecutedCommand, sensitiveValues),
            WorkingDirectory = SanitizeText(result.WorkingDirectory, sensitiveValues),
            ExitCode = result.ExitCode,
            TimedOut = result.TimedOut,
            IsDryRun = result.IsDryRun,
            IsSkipped = result.IsSkipped,
            IsConfigurationBlocked = result.IsConfigurationBlocked,
            RequiresCredentialPrompt = result.RequiresCredentialPrompt,
            RequiresExternalManualWorkflow = result.RequiresExternalManualWorkflow,
            WasInteractiveExecutionAttempted = result.WasInteractiveExecutionAttempted,
            ManualFallbackRecommended = result.ManualFallbackRecommended,
            StandardOutput = SanitizeText(result.StandardOutput, sensitiveValues),
            StandardError = SanitizeText(result.StandardError, sensitiveValues),
            StartedAt = result.StartedAt,
            Duration = result.Duration
        };
}