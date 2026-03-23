namespace WaptStudio.Core.Models;

public sealed class ValidationIssue
{
    public string Severity { get; set; } = "Info";

    public string Message { get; set; } = string.Empty;
}
