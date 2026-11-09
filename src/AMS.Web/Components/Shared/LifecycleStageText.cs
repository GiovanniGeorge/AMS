namespace AMS.Web.Components.Shared;

/// <summary>The words the screens use for an asset's lifecycle stage - one list for the whole app.</summary>
public static class LifecycleStageText
{
    /// <summary>"In store" instead of the enum's own "InStore". Called as <c>stage.Label()</c>.</summary>
    public static string Label(this LifecycleStage stage) => stage switch
    {
        LifecycleStage.InStore => "In store",
        LifecycleStage.Assigned => "Assigned",
        LifecycleStage.InTransit => "In transit",
        LifecycleStage.UnderMaintenance => "Under maintenance",
        LifecycleStage.Disposed => "Disposed",
        _ => stage.ToString()
    };
}
