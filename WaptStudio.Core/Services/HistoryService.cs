using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WaptStudio.Core.Configuration;
using WaptStudio.Core.Models;
using WaptStudio.Core.Services.Interfaces;

namespace WaptStudio.Core.Services;

public sealed class HistoryService : IHistoryService
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_initialized)
            {
                return;
            }

            AppPaths.EnsureCreated();

            await using var connection = new SqliteConnection($"Data Source={AppPaths.HistoryDatabasePath}");
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS history (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    ActionType TEXT NOT NULL,
                    PackageFolder TEXT NOT NULL,
                    PackageName TEXT NULL,
                    Success INTEGER NOT NULL,
                    Message TEXT NOT NULL,
                    ExecutedCommand TEXT NULL,
                    StandardOutput TEXT NULL,
                    StandardError TEXT NULL,
                    ExitCode INTEGER NOT NULL,
                    DurationMilliseconds INTEGER NOT NULL,
                    WindowsUser TEXT NOT NULL DEFAULT '',
                    VersionBefore TEXT NULL,
                    VersionAfter TEXT NULL,
                    WaptArtifactPath TEXT NULL,
                    ReadinessVerdict TEXT NULL
                );
                """;

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await EnsureColumnAsync(connection, "history", "ExecutedCommand", "TEXT NULL", cancellationToken).ConfigureAwait(false);
            await EnsureColumnAsync(connection, "history", "WindowsUser", "TEXT NOT NULL DEFAULT ''", cancellationToken).ConfigureAwait(false);
            await EnsureColumnAsync(connection, "history", "VersionBefore", "TEXT NULL", cancellationToken).ConfigureAwait(false);
            await EnsureColumnAsync(connection, "history", "VersionAfter", "TEXT NULL", cancellationToken).ConfigureAwait(false);
            await EnsureColumnAsync(connection, "history", "WaptArtifactPath", "TEXT NULL", cancellationToken).ConfigureAwait(false);
            await EnsureColumnAsync(connection, "history", "ReadinessVerdict", "TEXT NULL", cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task AddEntryAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection($"Data Source={AppPaths.HistoryDatabasePath}");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO history (
                Timestamp,
                ActionType,
                PackageFolder,
                PackageName,
                Success,
                Message,
                ExecutedCommand,
                StandardOutput,
                StandardError,
                ExitCode,
                DurationMilliseconds,
                WindowsUser,
                VersionBefore,
                    VersionAfter,
                    WaptArtifactPath,
                    ReadinessVerdict)
            VALUES (
                $timestamp,
                $actionType,
                $packageFolder,
                $packageName,
                $success,
                $message,
                $executedCommand,
                $standardOutput,
                $standardError,
                $exitCode,
                $durationMilliseconds,
                $windowsUser,
                $versionBefore,
                    $versionAfter,
                    $waptArtifactPath,
                    $readinessVerdict);
            """;
        command.Parameters.AddWithValue("$timestamp", entry.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("$actionType", entry.ActionType);
        command.Parameters.AddWithValue("$packageFolder", entry.PackageFolder);
        command.Parameters.AddWithValue("$packageName", (object?)entry.PackageName ?? DBNull.Value);
        command.Parameters.AddWithValue("$success", entry.Success ? 1 : 0);
        command.Parameters.AddWithValue("$message", entry.Message);
        command.Parameters.AddWithValue("$executedCommand", (object?)entry.ExecutedCommand ?? DBNull.Value);
        command.Parameters.AddWithValue("$standardOutput", (object?)entry.StandardOutput ?? DBNull.Value);
        command.Parameters.AddWithValue("$standardError", (object?)entry.StandardError ?? DBNull.Value);
        command.Parameters.AddWithValue("$exitCode", entry.ExitCode);
        command.Parameters.AddWithValue("$durationMilliseconds", entry.DurationMilliseconds);
        command.Parameters.AddWithValue("$windowsUser", entry.WindowsUser);
        command.Parameters.AddWithValue("$versionBefore", (object?)entry.VersionBefore ?? DBNull.Value);
        command.Parameters.AddWithValue("$versionAfter", (object?)entry.VersionAfter ?? DBNull.Value);
        command.Parameters.AddWithValue("$waptArtifactPath", (object?)entry.WaptArtifactPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$readinessVerdict", (object?)entry.ReadinessVerdict ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetRecentEntriesAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var entries = new List<HistoryEntry>();

        await using var connection = new SqliteConnection($"Data Source={AppPaths.HistoryDatabasePath}");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Timestamp, ActionType, PackageFolder, PackageName, Success, Message, ExecutedCommand, StandardOutput, StandardError, ExitCode, DurationMilliseconds, WindowsUser, VersionBefore, VersionAfter, WaptArtifactPath, ReadinessVerdict
            FROM history
            ORDER BY Id DESC
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new HistoryEntry
            {
                Id = reader.GetInt64(0),
                Timestamp = DateTimeOffset.Parse(reader.GetString(1)),
                ActionType = reader.GetString(2),
                PackageFolder = reader.GetString(3),
                PackageName = reader.IsDBNull(4) ? null : reader.GetString(4),
                Success = reader.GetInt32(5) == 1,
                Message = reader.GetString(6),
                ExecutedCommand = reader.IsDBNull(7) ? null : reader.GetString(7),
                StandardOutput = reader.IsDBNull(8) ? null : reader.GetString(8),
                StandardError = reader.IsDBNull(9) ? null : reader.GetString(9),
                ExitCode = reader.GetInt32(10),
                DurationMilliseconds = reader.GetInt32(11),
                WindowsUser = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                VersionBefore = reader.IsDBNull(13) ? null : reader.GetString(13),
                VersionAfter = reader.IsDBNull(14) ? null : reader.GetString(14),
                WaptArtifactPath = reader.IsDBNull(15) ? null : reader.GetString(15),
                ReadinessVerdict = reader.IsDBNull(16) ? null : reader.GetString(16)
            });
        }

        return entries;
    }

    public async Task<HistoryEntry?> GetEntryByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection($"Data Source={AppPaths.HistoryDatabasePath}");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Timestamp, ActionType, PackageFolder, PackageName, Success, Message, ExecutedCommand, StandardOutput, StandardError, ExitCode, DurationMilliseconds, WindowsUser, VersionBefore, VersionAfter, WaptArtifactPath, ReadinessVerdict
            FROM history
            WHERE Id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new HistoryEntry
        {
            Id = reader.GetInt64(0),
            Timestamp = DateTimeOffset.Parse(reader.GetString(1)),
            ActionType = reader.GetString(2),
            PackageFolder = reader.GetString(3),
            PackageName = reader.IsDBNull(4) ? null : reader.GetString(4),
            Success = reader.GetInt32(5) == 1,
            Message = reader.GetString(6),
            ExecutedCommand = reader.IsDBNull(7) ? null : reader.GetString(7),
            StandardOutput = reader.IsDBNull(8) ? null : reader.GetString(8),
            StandardError = reader.IsDBNull(9) ? null : reader.GetString(9),
            ExitCode = reader.GetInt32(10),
            DurationMilliseconds = reader.GetInt32(11),
            WindowsUser = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            VersionBefore = reader.IsDBNull(13) ? null : reader.GetString(13),
            VersionAfter = reader.IsDBNull(14) ? null : reader.GetString(14),
            WaptArtifactPath = reader.IsDBNull(15) ? null : reader.GetString(15),
            ReadinessVerdict = reader.IsDBNull(16) ? null : reader.GetString(16)
        };
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string tableName, string columnName, string columnDefinition, CancellationToken cancellationToken)
    {
        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = $"PRAGMA table_info({tableName});";

        await using var reader = await pragmaCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await reader.CloseAsync().ConfigureAwait(false);
        var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        await alterCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
