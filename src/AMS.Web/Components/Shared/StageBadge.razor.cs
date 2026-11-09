namespace AMS.Web.Components.Shared;

public partial class StageBadge
{
    [Parameter] public LifecycleStage Stage { get; set; }

    private string Label => Stage.Label();
}
