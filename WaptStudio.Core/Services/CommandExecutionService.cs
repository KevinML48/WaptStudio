using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;
using WaptStudio.Core.Utilities;

namespace WaptStudio.Core.Services;

public sealed class CommandExecutionService : ICommandExecutionService
{
    public async Task<CommandExecutionResult> ExecuteAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CommandExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Le nom de l'executable est requis.", nameof(fileName));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = !string.IsNullOrWhiteSpace(options?.StandardInputText),
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var startedAt = DateTimeOffset.Now;
        var stopwatch = Stopwatch.StartNew();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                standardOutput.AppendLine(eventArgs.Data);
            }
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                standardError.AppendLine(eventArgs.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Le processus n'a pas pu demarrer.");
            }

            if (!string.IsNullOrWhiteSpace(options?.StandardInputText))
            {
                await process.StandardInput.WriteAsync(options.StandardInputText).ConfigureAwait(false);
                await process.StandardInput.FlushAsync().ConfigureAwait(false);
                process.StandardInput.Close();
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryTerminateProcess(process);

                stopwatch.Stop();
                return SensitiveDataSanitizer.SanitizeCommandResult(new CommandExecutionResult
                {
                    FileName = fileName,
                    Arguments = arguments,
                    ExecutedCommand = $"{fileName} {arguments}".Trim(),
                    WorkingDirectory = workingDirectory,
                    ExitCode = -1,
                    TimedOut = true,
                    StandardOutput = standardOutput.ToString().Trim(),
                    StandardError = standardError.ToString().Trim(),
                    StartedAt = startedAt,
                    Duration = stopwatch.Elapsed
                }, options?.SensitiveValuesToRedact);
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return SensitiveDataSanitizer.SanitizeCommandResult(new CommandExecutionResult
            {
                FileName = fileName,
                Arguments = arguments,
                ExecutedCommand = $"{fileName} {arguments}".Trim(),
                WorkingDirectory = workingDirectory,
                ExitCode = process.ExitCode,
                TimedOut = false,
                StandardOutput = standardOutput.ToString().Trim(),
                StandardError = standardError.ToString().Trim(),
                StartedAt = startedAt,
                Duration = stopwatch.Elapsed
            }, options?.SensitiveValuesToRedact);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            stopwatch.Stop();

            return SensitiveDataSanitizer.SanitizeCommandResult(new CommandExecutionResult
            {
                FileName = fileName,
                Arguments = arguments,
                ExecutedCommand = $"{fileName} {arguments}".Trim(),
                WorkingDirectory = workingDirectory,
                ExitCode = -1,
                TimedOut = false,
                StandardOutput = standardOutput.ToString().Trim(),
                StandardError = string.IsNullOrWhiteSpace(standardError.ToString()) ? ex.Message : standardError.ToString().Trim(),
                StartedAt = startedAt,
                Duration = stopwatch.Elapsed
            }, options?.SensitiveValuesToRedact);
        }
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }
}
