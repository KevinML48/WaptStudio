namespace WaptStudio.Core.Models;

public sealed class ActionReadinessState
{
    public string ActionKey { get; init; } = string.Empty;

    public string ActionLabel { get; init; } = string.Empty;

    public ActionReadinessStatus Status { get; init; }

    public string Detail { get; init; } = string.Empty;

    public string StatusLabel => Status switch
    {
        ActionReadinessStatus.Available => "Disponible",
        ActionReadinessStatus.Configured => "Configure",
        ActionReadinessStatus.NotConfigured => "Non configure",
        ActionReadinessStatus.NotVerified => "Non verifie",
        ActionReadinessStatus.Tested => "Teste",
        ActionReadinessStatus.Validated => "Valide",
        _ => "Non disponible"
    };
}