using System.Collections.Generic;
using System.Linq;

namespace WaptStudio.Core.Models;

public sealed class ValidationResult
{
    public List<ValidationIssue> Issues { get; } = new();

    public CommandExecutionResult? CommandResult { get; set; }

    public bool HasErrors => Issues.Any(issue => string.Equals(issue.Severity, "ERROR", System.StringComparison.OrdinalIgnoreCase));

    public bool IsValid => Issues.All(issue => !string.Equals(issue.Severity, "ERROR", System.StringComparison.OrdinalIgnoreCase))
        && (CommandResult?.IsSuccess ?? true);

    public void AddOk(string message) => Issues.Add(new ValidationIssue { Severity = "OK", Message = message });

    public void AddError(string message) => Issues.Add(new ValidationIssue { Severity = "ERROR", Message = message });

    public void AddWarning(string message) => Issues.Add(new ValidationIssue { Severity = "WARNING", Message = message });

    public void AddInfo(string message) => Issues.Add(new ValidationIssue { Severity = "INFO", Message = message });
}
