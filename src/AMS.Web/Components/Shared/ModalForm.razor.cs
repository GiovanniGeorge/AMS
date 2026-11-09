namespace AMS.Web.Components.Shared;

public partial class ModalForm
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;

    /// <summary>Shown at the top of the dialog. The page keeps owning the message.</summary>
    [Parameter] public string? Error { get; set; }

    [Parameter] public string SaveText { get; set; } = "Save";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
