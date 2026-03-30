using System.Collections.Generic;

namespace WaptStudio.Core.Models;

public sealed class CommandExecutionOptions
{
    public string? StandardInputText { get; init; }

    public IReadOnlyList<string> SensitiveValuesToRedact { get; init; } = [];
}